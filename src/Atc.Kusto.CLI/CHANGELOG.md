# Changelog

## [4.0.0](https://github.com/atc-net/atc-kusto/compare/atc-kusto-cli@v3.5.1...atc-kusto-cli@v4.0.0) (2026-10-19)


### ⚠ BREAKING CHANGES

* **cli:** the atc-kusto tool needs the .NET 10 runtime; it no longer runs on .NET 9.

### New features

* **cli:** require the .NET 10 runtime ([dcac82b](https://github.com/atc-net/atc-kusto/commit/dcac82b48d43d53710c09d8cd42719c183c6dc52))
* **ingestion:** add Kusto Ingest V2 package reference ([bc3962c](https://github.com/atc-net/atc-kusto/commit/bc3962ca8ab71f33078dfd5adc83625d376d80c7))


### Bug fixes

* **cli:** reference the Kusto SDK directly instead of the Atc.Kusto library ([7f324b8](https://github.com/atc-net/atc-kusto/commit/7f324b86135e2a084d398ee5713c282429adcac5))

## [3.5.1](https://github.com/atc-net/atc-kusto/compare/atc-kusto@v3.5.0...atc-kusto@v3.5.1) (2026-04-18)


### Bug fixes

* **cli:** preserve resource path for ADE/ADX proxy cluster URLs ([74a7822](https://github.com/atc-net/atc-kusto/commit/74a78228632184cc42c1337b8e082024a5f37488))

## [3.5.0](https://github.com/atc-net/atc-kusto/compare/v3.4.0...v3.5.0) (2026-04-16)


### Features

* **cli:** add human-friendly value formatting for query results ([0265e49](https://github.com/atc-net/atc-kusto/commit/0265e4963d3c8da3a01fda4575f408bedae236c0))
* **cli:** add TSV query output format ([c2fecf8](https://github.com/atc-net/atc-kusto/commit/c2fecf8e9f837bd62edfda3359065207bcaf6ef9))

## [3.4.0](https://github.com/atc-net/atc-kusto/compare/v3.3.1...v3.4.0) (2026-03-31)


### Features

* **cli:** add cluster management with saved config and name resolution ([252b7ae](https://github.com/atc-net/atc-kusto/commit/252b7aeab0540621a4aa9d390788e3b51729b4ba))
* **cli:** add CSV output format for query command ([d20afc0](https://github.com/atc-net/atc-kusto/commit/d20afc0fb4c419716707c28929863078fd37dd03))
* **cli:** add database and table browsing commands with smart filtering ([314eeaf](https://github.com/atc-net/atc-kusto/commit/314eeafffaecb67de75a10ecd639ff84af37206e))
* **cli:** add database set-default and config-based resolution ([7546f16](https://github.com/atc-net/atc-kusto/commit/7546f169e7695f5114b78b8bce3691f911bb05b9))
* **cli:** add line-range support for query --file ([b31eb7d](https://github.com/atc-net/atc-kusto/commit/b31eb7d6f5298fa3257a5effe0c9d115d6b014e0))
* **cli:** add local KQL syntax validation before query execution ([63e7da7](https://github.com/atc-net/atc-kusto/commit/63e7da7e0f5b5f853bd1e326924be0e2b18680a7))
* **cli:** add query command with inline, file, and stdin support ([622be95](https://github.com/atc-net/atc-kusto/commit/622be95e9654525f16881ab06a233fe9befb6cdb))
* **cli:** add query statistics extraction with --show-stats flag ([ab3515e](https://github.com/atc-net/atc-kusto/commit/ab3515e17d63fb3a62f6df00d76b128427f6cdb5))
* **cli:** add web explorer deep-link URL generation ([445aaf0](https://github.com/atc-net/atc-kusto/commit/445aaf06fdfb31df345545973a5bd6b0b092ceaf))


### Bug Fixes

* **cli:** add CancellationToken to schema exporter and fix README gaps ([2733372](https://github.com/atc-net/atc-kusto/commit/27333728b012babf80111711ee0ac3677a3ce44a))
* **cli:** use JsonSerializer for proper JSON escaping in renderer ([6a3c3ca](https://github.com/atc-net/atc-kusto/commit/6a3c3ca616c9b3f7a2d4df9497871357b5d0b478))

## [3.3.0](https://github.com/atc-net/atc-kusto/compare/v3.2.0...v3.3.0) (2026-03-05)


### Features

* **cli:** add Kusto schema export CLI tool ([d1a6ba1](https://github.com/atc-net/atc-kusto/commit/d1a6ba192c14255712c0aa72ef77e008398eacfb))
