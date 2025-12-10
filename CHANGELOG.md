# Changelog

## [3.0.0](https://github.com/atc-net/atc-kusto/compare/v2.10.2...v3.0.0) (2025-12-10)


### ⚠ BREAKING CHANGES

* **analyzer:** split analyzer and code fix provider into separate assemblies
* **analyzer:** use AdditionalFiles instead of SyntaxTrees for kusto file detection

### Features

* add ATCK305 analyzer for empty kusto files ([7396416](https://github.com/atc-net/atc-kusto/commit/7396416d931110ea91f9faa67ba2c2ae688e0ec2))
* **analyzer:** add parameter validation for Kusto scripts ([3022211](https://github.com/atc-net/atc-kusto/commit/30222118c6818803264305787257eaefc8c5de49))
* **analyzer:** add projection validation rules for KustoQuery result contracts ([59c7b93](https://github.com/atc-net/atc-kusto/commit/59c7b934fe2c38cc765e47b613b0e4ae5d55a530))
* **analyzer:** create Atc.Kusto.Analyzer project structure ([b95699a](https://github.com/atc-net/atc-kusto/commit/b95699a99588ffbb2551f1c25925ddbccab007f8))
* **analyzer:** create stub .kusto file with code fix ([e4b59ae](https://github.com/atc-net/atc-kusto/commit/e4b59aee737ea791f61f10c7b36fddbc0dca95e5))
* **analyzer:** create test infrastructure ([f0341c1](https://github.com/atc-net/atc-kusto/commit/f0341c1bd056163090ef35865f525284a2078dee))
* **analyzer:** implement ATCK301 missing kusto script resource analyzer ([1f4a244](https://github.com/atc-net/atc-kusto/commit/1f4a244a355063b8f9175e0ccdd81659ccce0fd0))
* **analyzer:** implement code fix provider for ATCK301 ([fd1a2fb](https://github.com/atc-net/atc-kusto/commit/fd1a2fb28818cc2f100cc620b36e348588d48baf))
* **analyzer:** improve code fix provider with duplicate prevention and indentation support ([cf38d2d](https://github.com/atc-net/atc-kusto/commit/cf38d2d2a6cb87511fd38baf137ee8b298079830))
* extract common analyzer logic to KustoAnalyzerHelper ([75f4e3f](https://github.com/atc-net/atc-kusto/commit/75f4e3f0bd0025a5ab03ed185acdce9eaf0aef81))
* **nuget:** package analyzer with main library ([85a2547](https://github.com/atc-net/atc-kusto/commit/85a25472daadfb927eb647c7f6672886a1cd6983))


### Bug Fixes

* **analyzer:** add record declaration support and improve code fix for MissingKustoScriptResourceCodeFixProvider ([cb9b3c2](https://github.com/atc-net/atc-kusto/commit/cb9b3c2e055c83e4fdcad00486473fd2ddd0e3e7))
* **analyzer:** add regex timeouts to prevent DoS attacks in EmptyKustoScriptFileAnalyzer ([ba97474](https://github.com/atc-net/atc-kusto/commit/ba974749f03a00f4a730c28d6a2055cdb64fb0d6))
* **analyzer:** add regex timeouts to prevent DoS attacks in KustoParameterParser ([a207f28](https://github.com/atc-net/atc-kusto/commit/a207f28101b7c9ead315b6965784a46e065323f7))
* **analyzer:** ensure MissingKustoScriptResourceAnalyzer looks at RecordDeclarationSyntax ([ede4db2](https://github.com/atc-net/atc-kusto/commit/ede4db21d52c0c8c967aabf446f0f81ee5c906b8))
* **analyzer:** fix analyzer bundling with Atc.Kusto package ([ef66334](https://github.com/atc-net/atc-kusto/commit/ef663345179a97d9acc18950837e6916caaf297a))
* **analyzer:** use AdditionalFiles instead of SyntaxTrees for kusto file detection ([6e7845c](https://github.com/atc-net/atc-kusto/commit/6e7845c84ee0b15e86edc8cd6a7b7ee1f50cd7a7))
* **logging:** include query text and errors in SemanticException logs ([d5351f0](https://github.com/atc-net/atc-kusto/commit/d5351f0718a546439d84131d431b2b24dfbef02e))


### Code Refactoring

* **analyzer:** split analyzer and code fix provider into separate assemblies ([c3f09b4](https://github.com/atc-net/atc-kusto/commit/c3f09b4ff99c48f909bc5c84a8cea70639d28829))

## [2.10.2](https://github.com/atc-net/atc-kusto/compare/v2.10.1...v2.10.2) (2025-11-12)


### Bug Fixes

* add JToken handling to DataRowExtensions for dynamic fields ([dcaa4e0](https://github.com/atc-net/atc-kusto/commit/dcaa4e0e6caad8e0a36e020f704ff30480a75a61))

## [2.10.1](https://github.com/atc-net/atc-kusto/compare/v2.10.0...v2.10.1) (2025-10-27)


### Bug Fixes

* add KustoBooleanJsonConverter to handle Kusto's numeric boolean values ([587e4cc](https://github.com/atc-net/atc-kusto/commit/587e4cc0e7b488eab8d0e94e2139706934f4d0cb))

## [2.10.0](https://github.com/atc-net/atc-kusto/compare/v2.9.0...v2.10.0) (2025-10-21)


### Features

* **extensions:** add SqlDecimal to decimal conversion helper ([aed3d3d](https://github.com/atc-net/atc-kusto/commit/aed3d3d1a3662310740745e1d6a0f372aa80a16d))


### Bug Fixes

* **streaming:** convert progressive frame values to match column types ([ef9d846](https://github.com/atc-net/atc-kusto/commit/ef9d846c962f8e8da85eb9efe3aef61284024361))

## [2.9.0](https://github.com/atc-net/atc-kusto/compare/v2.8.0...v2.9.0) (2025-09-29)


### Features

* add cancellation exception normalization for streaming queries ([2f25423](https://github.com/atc-net/atc-kusto/commit/2f254234804dfd30d44de9134aa3354b5372e94f))
* add cancellation exception normalization to query handlers ([e106693](https://github.com/atc-net/atc-kusto/commit/e10669386e35ea3265990e1874da8c0cf3458dad))
* add exception normalization helpers for cancellation ([b0ea232](https://github.com/atc-net/atc-kusto/commit/b0ea23209f7ff0e4ef238d946ef6f9f6af6a6759))
* add server-side cancellation support for Kusto queries ([ff042e5](https://github.com/atc-net/atc-kusto/commit/ff042e594019f870e987e02f626ad86e7b3f9c70))


### Bug Fixes

* prevent retry on cancellation exceptions in resilience pipeline ([1a4d6fd](https://github.com/atc-net/atc-kusto/commit/1a4d6fd1758f2acf10fbe9d3aac400fc613e81e8))

## [2.8.0](https://github.com/atc-net/atc-kusto/compare/v2.7.0...v2.8.0) (2025-09-14)


### Features

* **samples:** switch customer sales values to decimal and adjust queries ([5188823](https://github.com/atc-net/atc-kusto/commit/518882372a2068b1797c75f19547095a08596973))
* **serialization:** add decimal converter and update serializer docs ([5f7fad6](https://github.com/atc-net/atc-kusto/commit/5f7fad6c3dbf8fe052839816679d5cdff4b5773f))
* **serialization:** enhance decimal converter robustness and add extended tests ([44ebac2](https://github.com/atc-net/atc-kusto/commit/44ebac225e700394739b7a775d3e208eb71c207c))

## [2.7.0](https://github.com/atc-net/atc-kusto/compare/v2.6.1...v2.7.0) (2025-08-13)


### Features

* upgrade Microsoft.Azure.Kusto.Data package to newest major version ([19be30a](https://github.com/atc-net/atc-kusto/commit/19be30a1a95181aadd33fd67d4c7f7c1542decc2))

## [2.6.1](https://github.com/atc-net/atc-kusto/compare/v2.6.0...v2.6.1) (2025-06-24)


### Bug Fixes

* avoid unicode characters in KustoClientProvider exception message ([e457fbc](https://github.com/atc-net/atc-kusto/commit/e457fbc730c5ab9b8289d01ccfa67d1c7eb04c4d))
* **healthcheck:** ensure camelCase is used for data ([5da9541](https://github.com/atc-net/atc-kusto/commit/5da9541a63d6eaaeefd81f4c8b3e51b6bf1650a2))

## [2.6.0](https://github.com/atc-net/atc-kusto/compare/v2.5.0...v2.6.0) (2025-06-23)


### Features

* add KustoClusterDiagnostic healthcheck support ([defbce0](https://github.com/atc-net/atc-kusto/commit/defbce07d7a4257fb5c62c01711c66888810ac58))
* **sample:** add sample for healthcheck usage ([efd63df](https://github.com/atc-net/atc-kusto/commit/efd63df38037c986d92b1d6e5f26125a68422832))

## [2.5.0](https://github.com/atc-net/atc-kusto/compare/v2.4.1...v2.5.0) (2025-06-04)


### Features

* add support for connectionstrings in KustoClientProvider/AtcKustoOptions incl. unit-tests ([2ebb223](https://github.com/atc-net/atc-kusto/commit/2ebb22346961b36dc1f831fa431a49d42a677e23))

## [2.4.1](https://github.com/atc-net/atc-kusto/compare/v2.4.0...v2.4.1) (2025-05-26)


### Bug Fixes

* ensure KustoProcessor has an overload for ExecuteQuery without QueryOptions ([e9e0e0b](https://github.com/atc-net/atc-kusto/commit/e9e0e0bdb380b50358ad2cb1d90a9f0d76ccc870))

## [2.4.0](https://github.com/atc-net/atc-kusto/compare/v2.3.0...v2.4.0) (2025-05-23)


### Features

* add streaming support via StreamingQueryHandler and BufferedStreamingQueryHandler ([4477081](https://github.com/atc-net/atc-kusto/commit/44770819ecb6f1cd81a3ca7f264f68ee7c0917f4))

## [2.3.0](https://github.com/atc-net/atc-kusto/compare/v2.2.1...v2.3.0) (2025-04-11)


### Features

* disable pagination when no pageSize is specified ([b60c64c](https://github.com/atc-net/atc-kusto/commit/b60c64c921e8089b89f51a5cf9ba9b0df1cd4bd4))

## [2.2.1](https://github.com/atc-net/atc-kusto/compare/v2.2.0...v2.2.1) (2025-03-25)


### Bug Fixes

* ensure IKustoClientProvider is public ([3301efe](https://github.com/atc-net/atc-kusto/commit/3301efea962ac05fe06ff0926abcadb02113cc0f))

## [2.2.0](https://github.com/atc-net/atc-kusto/compare/v2.1.0...v2.2.0) (2025-03-25)


### Features

* add ResiliencePipeline to ExistingPagedStoredQueryHandler and SimpleQueryHandler ([598ed66](https://github.com/atc-net/atc-kusto/commit/598ed6626f7c642c1840338f763873a60e82d76c))

## [2.1.0](https://github.com/atc-net/atc-kusto/compare/v2.0.1...v2.1.0) (2025-03-19)


### Features

* extend ServiceCollectionExtensions for ConfigureAzureDataExplorer to take optional configurationName ([be230ba](https://github.com/atc-net/atc-kusto/commit/be230ba1f391d572830b441181093ebace724a75))
* introduce KustoClientProvider, which provides functionality to retrieve Kusto clients for query executions or admin operations based on optional connectionName and databaseName ([326865e](https://github.com/atc-net/atc-kusto/commit/326865e581900208c03c2c76e1be61fe6de9bc9c))
* introduce KustoProcessorFactory for creating instances of kustoprocessor configurable with specified connectionName and/or databaseName ([0afe917](https://github.com/atc-net/atc-kusto/commit/0afe91776dcc52591443661fc502874d313ccc4a))
* make KustoScript more extensible by marking GetQueryText and GetParameters as virtual ([9d1e7f0](https://github.com/atc-net/atc-kusto/commit/9d1e7f07f6cf72baddc423ded10b4251c5974373))

## [2.0.1](https://github.com/atc-net/atc-kusto/compare/v2.0.0...v2.0.1) (2025-01-26)


### Bug Fixes

* ensure credential is an optional part of KustoConnectionStringBuilder to be able to connect to kustainer container image ([db7cf8a](https://github.com/atc-net/atc-kusto/commit/db7cf8a81251311bcabb88de69646d41d94bec72))
* set IsPackable to false on sample project to avoid nuget package generation ([4a2f7cd](https://github.com/atc-net/atc-kusto/commit/4a2f7cd360cfa279eee2108c48c61522ffdab522))

## [2.0.0](https://github.com/atc-net/atc-kusto/compare/v1.0.8...v2.0.0) (2025-01-09)


### ⚠ BREAKING CHANGES

* change KustoQuery.ReadResult to virtual method
* upgrade to .net9

### Features

* add dark mode for swagger in sample api ([5a63e22](https://github.com/atc-net/atc-kusto/commit/5a63e22e71ec2572da0ffb6e955cfea3a71a49aa))
* add sample api ([0e44013](https://github.com/atc-net/atc-kusto/commit/0e440138873fabe37f017813fefbd654826236a5))
* add sample to coding-rules configuration and update static code analyzers and coding-rules ([b509549](https://github.com/atc-net/atc-kusto/commit/b509549641a0cd8d205510300f52aaf7bda765cf))
* add several ServiceCollectionExtension overloads to ease configuration of AzureDataExplorer ([3f699c5](https://github.com/atc-net/atc-kusto/commit/3f699c52d27b0b615f9b35ce854299171a7295df))
* change KustoQuery.ReadResult to virtual method ([106c942](https://github.com/atc-net/atc-kusto/commit/106c94230fbf0a0ccb13c0022ff5e638a9e944b8))
* **sample:** add console sample application ([8f91808](https://github.com/atc-net/atc-kusto/commit/8f91808f9fa245256f9312182225adc0c85b7453))
* upgrade to .net9 ([f8adf71](https://github.com/atc-net/atc-kusto/commit/f8adf7151a58b7c0673e961d357fae795a337a0e))
