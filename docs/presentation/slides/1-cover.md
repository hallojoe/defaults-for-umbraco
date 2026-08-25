---
class: text-white
---

<img class="cover-image" src="/topology-cover.png" alt="Local Umbraco CM/CD topology" />

<div class="cover-overlay" />

<div class="cover-content">

# The interesting bits<br>of something boring

## Running Umbraco 17 locally with .NET Aspire

Production-like CM/CD topology, without making local development painful.

</div>

<style scoped>
.cover-image,
.cover-overlay {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
}

.cover-image {
  object-fit: cover;
  z-index: 0;
}

.cover-overlay {
  background: linear-gradient(90deg, rgba(2, 6, 23, 0.88) 0%, rgba(2, 6, 23, 0.48) 43%, rgba(2, 6, 23, 0.05) 75%);
  z-index: 1;
}

.cover-content {
  position: relative;
  z-index: 2;
  max-width: 52%;
  padding-top: 12%;
}

.cover-content :deep(h1) {
  font-size: 2.65rem;
  line-height: 1.08;
}

.cover-content :deep(h2) {
  font-size: 1.35rem;
  line-height: 1.25;
}

.cover-content :deep(p) {
  font-size: 1.05rem;
}
</style>
