# Vietnamese administrative address catalog

Source: https://provinces.open-api.vn/api/v2/?depth=2
Documentation: https://provinces.open-api.vn/
Retrieved: 2026-10-12
Catalog: 34 provinces/cities and 3,321 commune-level units (including special zones).
API v2 represents the administrative structure after the July 2025 merger.

The catalog is bundled with the application to avoid runtime dependence on an external API. It is a dated snapshot, not an automatic synchronization feed. To update, download API v2 data, run `node --test tests/vietnam-address.test.cjs`, and review any changed codes or counts before deployment. Existing patient addresses retain their saved names and codes. Legacy free-text addresses are preserved without guessing their province or ward.
