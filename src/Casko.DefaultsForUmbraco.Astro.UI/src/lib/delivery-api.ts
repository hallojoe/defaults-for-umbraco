export type PageKind = 'home' | 'list' | 'content';

export interface SitePage {
  id: string;
  culture: string;
  name: string;
  title: string;
  description?: string;
  contentMarkup?: string;
  routePath: string;
  parentPath?: string;
  kind: PageKind;
}

export interface DeliverySite {
  pages: SitePage[];
  cultures: string[];
}

interface ApiRoute { path: string; }
interface ApiRichText { markup?: string | null; }
interface ApiProperties {
  title?: string | null;
  description?: string | null;
  content?: ApiRichText | null;
}
interface ApiContent {
  id: string;
  name?: string | null;
  route: ApiRoute;
  cultures: Record<string, ApiRoute>;
  properties: ApiProperties;
}
interface PagedContent { total: number; items: ApiContent[]; }

const defaultCulture = 'da';
const defaultApiUrl = 'https://cd.dev.localhost:4443';
const defaultStartItem = 'b04a32de-a3c4-4946-9ce4-efb9bbedb396';
const contentFields = 'properties[title,description,content]';
const pageSize = 100;

const apiUrl = (import.meta.env.UMBRACO_DELIVERY_API_URL ?? defaultApiUrl).replace(/\/$/, '');
const startItem = import.meta.env.UMBRACO_DELIVERY_START_ITEM ?? defaultStartItem;
const apiKey = import.meta.env.UMBRACO_DELIVERY_API_KEY;

function pathToUrl(path: string): string {
  const withLeadingSlash = path.startsWith('/') ? path : `/${path}`;
  return withLeadingSlash.endsWith('/') ? withLeadingSlash : `${withLeadingSlash}/`;
}

function parentPath(path: string): string | undefined {
  const segments = pathToUrl(path).split('/').filter(Boolean);
  if (segments.length === 0) return undefined;
  return segments.length === 1 ? '/' : `/${segments.slice(0, -1).join('/')}/`;
}

function valueOrUndefined(value: string | null | undefined): string | undefined {
  return value?.trim() || undefined;
}

function markupOrUndefined(content: ApiRichText | null | undefined): string | undefined {
  return valueOrUndefined(content?.markup);
}

function createHeaders(culture: string, scopedToStartItem: boolean): Headers {
  const headers = new Headers({ 'Accept-Language': culture });
  if (apiKey) headers.set('Api-Key', apiKey);
  if (scopedToStartItem) headers.set('Start-Item', startItem);
  return headers;
}

async function deliveryFetch<T>(path: string, culture: string, scopedToStartItem: boolean): Promise<T> {
  let response: Response;

  try {
    response = await fetch(`${apiUrl}${path}`, {
      headers: createHeaders(culture, scopedToStartItem),
    });
  } catch (error) {
    const reason = error instanceof Error ? error.message : String(error);
    throw new Error(
      `Delivery API request could not reach ${apiUrl} (${reason}). ` +
        'Check UMBRACO_DELIVERY_API_URL, certificate trust, and that the Delivery API is running.',
    );
  }

  if (!response.ok) {
    throw new Error(
      `Delivery API request failed (${response.status} ${response.statusText}) for ${path}. ` +
        'Check UMBRACO_DELIVERY_API_URL, UMBRACO_DELIVERY_START_ITEM, and UMBRACO_DELIVERY_API_KEY.',
    );
  }

  return response.json() as Promise<T>;
}

async function getRoot(culture: string): Promise<ApiContent> {
  const parameters = new URLSearchParams({ fields: contentFields });
  return deliveryFetch<ApiContent>(`/umbraco/delivery/api/v2/content/item/${startItem}?${parameters}`, culture, false);
}

async function getDescendants(culture: string): Promise<ApiContent[]> {
  const items: ApiContent[] = [];
  let skip = 0;
  let total = Number.POSITIVE_INFINITY;

  while (skip < total) {
    const parameters = new URLSearchParams({ fields: contentFields, skip: String(skip), take: String(pageSize) });
    const page = await deliveryFetch<PagedContent>(`/umbraco/delivery/api/v2/content?${parameters}`, culture, true);
    items.push(...page.items);
    total = page.total;

    if (page.items.length === 0 && skip < total) {
      throw new Error('Delivery API returned an incomplete paged response while building the static site.');
    }

    skip += page.items.length;
  }

  return items;
}

async function getContentByPath(path: string, culture: string, rootPath: string): Promise<ApiContent> {
  const relativePath = pathToUrl(path).slice(pathToUrl(rootPath).length).replace(/\/$/, '');
  const parameters = new URLSearchParams({ fields: contentFields });
  return deliveryFetch<ApiContent>(
    `/umbraco/delivery/api/v2/content/item/${relativePath}?${parameters}`,
    culture,
    true,
  );
}

async function resolveRouteCollisions(
  descendants: ApiContent[],
  culture: string,
  rootPath: string,
): Promise<ApiContent[]> {
  const itemsByRoute = Map.groupBy(descendants, (item) => pathToUrl(item.route.path));
  const canonicalItems = await Promise.all(
    [...itemsByRoute.entries()].map(async ([routePath, items]) => {
      if (items.length === 1) return items[0];
      return getContentByPath(routePath, culture, rootPath);
    }),
  );

  return canonicalItems;
}

function normalisePage(item: ApiContent, culture: string, rootPath: string, defaultItem?: ApiContent): SitePage {
  const routePath = pathToUrl(item.route.path);
  const itemParentPath = parentPath(routePath);
  const isRoot = item.id === startItem;
  const name = valueOrUndefined(item.name) ?? valueOrUndefined(defaultItem?.name) ?? 'Untitled page';
  const title = valueOrUndefined(item.properties.title) ?? valueOrUndefined(defaultItem?.properties.title) ?? name;

  return {
    id: item.id,
    culture,
    name,
    title,
    description: valueOrUndefined(item.properties.description) ?? valueOrUndefined(defaultItem?.properties.description),
    contentMarkup: markupOrUndefined(item.properties.content) ?? markupOrUndefined(defaultItem?.properties.content),
    routePath,
    parentPath: itemParentPath,
    kind: isRoot ? 'home' : itemParentPath === rootPath ? 'list' : 'content',
  };
}

async function buildSite(): Promise<DeliverySite> {
  const defaultRoot = await getRoot(defaultCulture);
  const cultures = Object.keys(defaultRoot.cultures).sort((left, right) => {
    if (left === defaultCulture) return -1;
    if (right === defaultCulture) return 1;
    return left.localeCompare(right);
  });

  if (!cultures.includes(defaultCulture)) {
    throw new Error(`The selected Delivery API root must be published in the default culture '${defaultCulture}'.`);
  }

  const contentByCulture = await Promise.all(cultures.map(async (culture) => ({
    culture,
    root: await getRoot(culture),
    descendants: await getDescendants(culture),
  })));
  const defaultContent = contentByCulture.find(({ culture }) => culture === defaultCulture)!;
  const defaultItems = new Map<string, ApiContent>();
  defaultItems.set(defaultContent.root.id, defaultContent.root);
  defaultContent.descendants.forEach((item) => defaultItems.set(item.id, item));

  const pages: SitePage[] = [];

  for (const { culture, root, descendants } of contentByCulture) {
    const rootPath = pathToUrl(root.route.path);
    const canonicalDescendants = await resolveRouteCollisions(descendants, culture, rootPath);
    const localizedItems = [root, ...canonicalDescendants].filter(
      (item) => item.id === startItem || item.cultures[culture],
    );
    pages.push(...localizedItems.map((item) => normalisePage(item, culture, rootPath, defaultItems.get(item.id))));
  }

  return { cultures, pages };
}

let sitePromise: Promise<DeliverySite> | undefined;

export function getDeliverySite(): Promise<DeliverySite> {
  sitePromise ??= buildSite();
  return sitePromise;
}

export function getRootPages(site: DeliverySite): SitePage[] {
  return site.pages.filter((page) => page.kind === 'home');
}

export function getChildPages(page: SitePage, pages: SitePage[]): SitePage[] {
  return pages.filter((candidate) => candidate.culture === page.culture && candidate.parentPath === page.routePath);
}

export function getBreadcrumbs(page: SitePage, pages: SitePage[]): SitePage[] {
  const breadcrumbs: SitePage[] = [];
  let currentPath: string | undefined = page.routePath;

  while (currentPath) {
    const current = pages.find((candidate) => candidate.culture === page.culture && candidate.routePath === currentPath);
    if (!current) break;
    breadcrumbs.unshift(current);
    currentPath = current.parentPath;
  }

  return breadcrumbs;
}
