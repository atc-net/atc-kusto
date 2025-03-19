# Changelog

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
