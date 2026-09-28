export type PageKind = 'home' | 'area' | 'group' | 'text' | 'event' | 'product' | 'tags' | 'search' | 'generic';

export interface SitePage {
  id: string;
  culture: string;
  contentType: string;
  name: string;
  title: string;
  description?: string;
  contentMarkup?: string;
  routePath: string;
  parentPath?: string;
  kind: PageKind;
  tags: string[];
  eventStartDate?: string;
  eventEndDate?: string;
  contactEmail?: string;
  contactTelephone?: string;
  productName?: string;
  productDescriptionMarkup?: string;
  productImageUrl?: string;
  productImageAlt?: string;
  productSku?: string;
  productBrand?: string;
  productCategory?: string;
}

export interface DeliverySite { pages: SitePage[]; cultures: string[]; }

interface ApiRoute { path: string; }
interface ApiRichText { markup?: string | null; }
interface ApiMedia { url?: string | null; mediaUrl?: string | null; name?: string | null; properties?: { altText?: string | null }; }
interface ApiProperties {
  title?: string | null; description?: string | null; content?: ApiRichText | null; tags?: string[] | null;
  eventStartDate?: string | null; eventEndDate?: string | null; contactPointEmail?: string | null; contactPointTelephone?: string | null;
  productName?: string | null; productDescription?: ApiRichText | null; productImage?: ApiMedia | ApiMedia[] | null;
  productSku?: string | null; productBrand?: string | null; productCategory?: string | null;
}
interface ApiContent { id: string; contentType: string; name?: string | null; route: ApiRoute; cultures: Record<string, ApiRoute>; properties: ApiProperties; }
interface PagedContent { total: number; items: ApiContent[]; }

const defaultCulture = 'da';
const defaultApiUrl = 'https://cd.dev.localhost:4443';
const defaultStartItem = 'b04a32de-a3c4-4946-9ce4-efb9bbedb396';
const contentFields = 'properties[title,description,content,tags,eventStartDate,eventEndDate,contactPointEmail,contactPointTelephone,productName,productDescription,productImage,productSku,productBrand,productCategory]';
const pageSize = 100;
const apiUrl = (import.meta.env.UMBRACO_DELIVERY_API_URL ?? defaultApiUrl).replace(/\/$/, '');
const mediaBaseUrl = (import.meta.env.UMBRACO_MEDIA_BASE_URL ?? new URL(apiUrl).origin).replace(/\/$/, '');
const startItem = import.meta.env.UMBRACO_DELIVERY_START_ITEM ?? defaultStartItem;
const apiKey = import.meta.env.UMBRACO_DELIVERY_API_KEY;

function pathToUrl(path: string): string { const value = path.startsWith('/') ? path : `/${path}`; return value.endsWith('/') ? value : `${value}/`; }
function parentPath(path: string): string | undefined { const segments = pathToUrl(path).split('/').filter(Boolean); return segments.length === 0 ? undefined : segments.length === 1 ? '/' : `/${segments.slice(0, -1).join('/')}/`; }
function valueOrUndefined(value: string | null | undefined): string | undefined { return value?.trim() || undefined; }

export function resolveMediaUrl(value: string | null | undefined): string | undefined {
  const url = valueOrUndefined(value);
  if (!url || /^(?:[a-z][a-z\d+.-]*:|\/\/|#)/i.test(url)) return url;
  return new URL(url, `${mediaBaseUrl}/`).toString();
}

function rewriteSrcSet(value: string): string { return value.split(',').map((entry) => { const [url, ...descriptor] = entry.trim().split(/\s+/); return [resolveMediaUrl(url) ?? url, ...descriptor].join(' '); }).join(', '); }

export function rewriteRichTextMedia(markup: string | null | undefined): string | undefined {
  const html = valueOrUndefined(markup);
  if (!html) return undefined;
  return html
    .replace(/\b(src|poster)\s*=\s*(["'])(.*?)\2/gi, (_match, attribute, quote, value) => `${attribute}=${quote}${resolveMediaUrl(value) ?? value}${quote}`)
    .replace(/\bsrcset\s*=\s*(["'])(.*?)\1/gi, (_match, quote, value) => `srcset=${quote}${rewriteSrcSet(value)}${quote}`);
}

function markupOrUndefined(content: ApiRichText | null | undefined): string | undefined { return rewriteRichTextMedia(content?.markup); }
function mediaOrUndefined(value: ApiMedia | ApiMedia[] | null | undefined): ApiMedia | undefined { return Array.isArray(value) ? value[0] : value ?? undefined; }
function createHeaders(culture: string, scoped: boolean): Headers { const headers = new Headers({ 'Accept-Language': culture }); if (apiKey) headers.set('Api-Key', apiKey); if (scoped) headers.set('Start-Item', startItem); return headers; }

async function deliveryFetch<T>(path: string, culture: string, scoped: boolean): Promise<T> {
  let response: Response;
  try { response = await fetch(`${apiUrl}${path}`, { headers: createHeaders(culture, scoped) }); }
  catch (error) { throw new Error(`Delivery API request could not reach ${apiUrl} (${error instanceof Error ? error.message : String(error)}). Check UMBRACO_DELIVERY_API_URL and certificate trust.`); }
  if (!response.ok) throw new Error(`Delivery API request failed (${response.status} ${response.statusText}) for ${path}. Check Delivery API configuration.`);
  return response.json() as Promise<T>;
}

async function getRoot(culture: string): Promise<ApiContent> { return deliveryFetch<ApiContent>(`/umbraco/delivery/api/v2/content/item/${startItem}?${new URLSearchParams({ fields: contentFields, expand: 'properties[productImage]' })}`, culture, false); }
async function getDescendants(culture: string): Promise<ApiContent[]> {
  const items: ApiContent[] = []; let skip = 0; let total = Number.POSITIVE_INFINITY;
  while (skip < total) { const page = await deliveryFetch<PagedContent>(`/umbraco/delivery/api/v2/content?${new URLSearchParams({ fields: contentFields, expand: 'properties[productImage]', skip: String(skip), take: String(pageSize) })}`, culture, true); items.push(...page.items); total = page.total; /* The total can include variants unavailable in this culture; a short page is the reliable end marker. */ if (page.items.length < pageSize) break; skip += page.items.length; }
  return items;
}
async function getContentByPath(path: string, culture: string, rootPath: string): Promise<ApiContent> { const itemPath = pathToUrl(path); const relativePath = itemPath.slice(pathToUrl(rootPath).length).replace(/\/$/, ''); return deliveryFetch<ApiContent>(`/umbraco/delivery/api/v2/content/item/${relativePath}?${new URLSearchParams({ fields: contentFields, expand: 'properties[productImage]' })}`, culture, true); }
async function resolveRouteCollisions(descendants: ApiContent[], culture: string, rootPath: string): Promise<ApiContent[]> { const byRoute = Map.groupBy(descendants, (item) => pathToUrl(item.route.path)); return Promise.all([...byRoute.entries()].map(async ([route, items]) => items.length === 1 ? items[0] : getContentByPath(route, culture, rootPath))); }

function pageKind(contentType: string, isRoot: boolean): PageKind {
  if (isRoot) return 'home';
  return ({ areaPage: 'area', groupPage: 'group', textPage: 'text', eventPage: 'event', productPage: 'product', tagsPage: 'tags', searchPage: 'search' } as Record<string, PageKind>)[contentType] ?? 'generic';
}

function normalisePage(item: ApiContent, culture: string, defaultItem?: ApiContent): SitePage {
  const properties = item.properties; const fallback = defaultItem?.properties; const routePath = pathToUrl(item.route.path); const media = mediaOrUndefined(properties.productImage ?? fallback?.productImage);
  const name = valueOrUndefined(item.name) ?? valueOrUndefined(defaultItem?.name) ?? 'Untitled page';
  const productName = valueOrUndefined(properties.productName) ?? valueOrUndefined(fallback?.productName);
  return {
    id: item.id, culture, contentType: item.contentType, name, title: valueOrUndefined(properties.title) ?? valueOrUndefined(fallback?.title) ?? productName ?? name,
    description: valueOrUndefined(properties.description) ?? valueOrUndefined(fallback?.description), contentMarkup: markupOrUndefined(properties.content) ?? markupOrUndefined(fallback?.content),
    routePath, parentPath: parentPath(routePath), kind: pageKind(item.contentType, item.id === startItem), tags: properties.tags ?? fallback?.tags ?? [],
    eventStartDate: valueOrUndefined(properties.eventStartDate) ?? valueOrUndefined(fallback?.eventStartDate), eventEndDate: valueOrUndefined(properties.eventEndDate) ?? valueOrUndefined(fallback?.eventEndDate),
    contactEmail: valueOrUndefined(properties.contactPointEmail) ?? valueOrUndefined(fallback?.contactPointEmail), contactTelephone: valueOrUndefined(properties.contactPointTelephone) ?? valueOrUndefined(fallback?.contactPointTelephone),
    productName, productDescriptionMarkup: markupOrUndefined(properties.productDescription) ?? markupOrUndefined(fallback?.productDescription), productImageUrl: resolveMediaUrl(media?.url ?? media?.mediaUrl), productImageAlt: valueOrUndefined(media?.properties?.altText) ?? valueOrUndefined(media?.name),
    productSku: valueOrUndefined(properties.productSku) ?? valueOrUndefined(fallback?.productSku), productBrand: valueOrUndefined(properties.productBrand) ?? valueOrUndefined(fallback?.productBrand), productCategory: valueOrUndefined(properties.productCategory) ?? valueOrUndefined(fallback?.productCategory),
  };
}

async function buildSite(): Promise<DeliverySite> {
  const defaultRoot = await getRoot(defaultCulture); const cultures = Object.keys(defaultRoot.cultures).sort((a, b) => a === defaultCulture ? -1 : b === defaultCulture ? 1 : a.localeCompare(b));
  if (!cultures.includes(defaultCulture)) throw new Error(`The selected Delivery API root must be published in '${defaultCulture}'.`);
  const all = await Promise.all(cultures.map(async (culture) => ({ culture, root: await getRoot(culture), descendants: await getDescendants(culture) })));
  const defaultContent = all.find((entry) => entry.culture === defaultCulture)!; const fallback = new Map<string, ApiContent>([[defaultContent.root.id, defaultContent.root], ...defaultContent.descendants.map((item) => [item.id, item])]);
  return { cultures, pages: (await Promise.all(all.map(async ({ culture, root, descendants }) => { const descendantsForCulture = await resolveRouteCollisions(descendants, culture, root.route.path); return [root, ...descendantsForCulture].filter((item) => item.id === startItem || item.cultures[culture]).map((item) => normalisePage(item, culture, fallback.get(item.id))); }))).flat() };
}

let sitePromise: Promise<DeliverySite> | undefined;
export function getDeliverySite(): Promise<DeliverySite> { sitePromise ??= buildSite(); return sitePromise; }
export function getRootPages(site: DeliverySite): SitePage[] { return site.pages.filter((page) => page.kind === 'home'); }
export function getChildPages(page: SitePage, pages: SitePage[]): SitePage[] { return pages.filter((candidate) => candidate.culture === page.culture && candidate.parentPath === page.routePath); }
export function getBreadcrumbs(page: SitePage, pages: SitePage[]): SitePage[] { const result: SitePage[] = []; let path: string | undefined = page.routePath; while (path) { const current = pages.find((candidate) => candidate.culture === page.culture && candidate.routePath === path); if (!current) break; result.unshift(current); path = current.parentPath; } return result; }
