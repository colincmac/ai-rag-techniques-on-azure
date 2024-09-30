
var chatContainer = {
  name: 'chatContainer'
  properties: {
    indexingPolicy: {
      automatic: true
    }
    partitionKey: {
      paths: [
        '/id'
      ]
      kind: 'Hash'
    }
    uniqueKeyPolicy: {
      uniqueKeys: [
        {
          paths: [
            '/name'
          ]
        }
      ]
    }
  }
}
module databaseAccount 'br/public:avm/res/document-db/database-account:0.6.1' = {
  name: 'databaseAccountDeployment'
  params: {
    // Required parameters
    name: 'woodgrove-eastus2'
    // Non-required parameters
    location: 'EastUS2'
    enableTelemetry: false
    sqlDatabases: [
      {
        containers: [
          {
            analyticalStorageTtl: 0
            conflictResolutionPolicy: {
              conflictResolutionPath: '/myCustomId'
              mode: 'LastWriterWins'
            }
            defaultTtl: 1000
            indexingPolicy: {
              automatic: true
            }
            kind: 'Hash'
            name: 'container-001'
            paths: [
              '/myPartitionKey'
            ]
            throughput: 600
            uniqueKeyPolicyKeys: [
              {
                paths: [
                  '/firstName'
                ]
              }
              {
                paths: [
                  '/lastName'
                ]
              }
            ]
          }
        ]
        name: 'all-configs-specified'
      }
      {
        containers: [
          {
            indexingPolicy: {
              automatic: true
            }
            name: 'container-001'
            paths: [
              '/myPartitionKey'
            ]
          }
        ]
        name: 'automatic-indexing-policy'
      }
      {
        containers: [
          {
            conflictResolutionPolicy: {
              conflictResolutionPath: '/myCustomId'
              mode: 'LastWriterWins'
            }
            name: 'container-001'
            paths: [
              '/myPartitionKey'
            ]
          }
        ]
        name: 'last-writer-conflict-resolution-policy'
      }
      {
        containers: [
          {
            analyticalStorageTtl: 1000
            name: 'container-001'
            paths: [
              '/myPartitionKey'
            ]
          }
        ]
        name: 'fixed-analytical-ttl'
      }
      {
        containers: [
          {
            analyticalStorageTtl: -1
            name: 'container-001'
            paths: [
              '/myPartitionKey'
            ]
          }
        ]
        name: 'infinite-analytical-ttl'
      }
      {
        containers: [
          {
            defaultTtl: 1000
            name: 'container-001'
            paths: [
              '/myPartitionKey'
            ]
          }
        ]
        name: 'document-ttl'
      }
      {
        containers: [
          {
            name: 'container-001'
            paths: [
              '/myPartitionKey'
            ]
            uniqueKeyPolicyKeys: [
              {
                paths: [
                  '/firstName'
                ]
              }
              {
                paths: [
                  '/lastName'
                ]
              }
            ]
          }
        ]
        name: 'unique-key-policy'
      }
      {
        containers: [
          {
            name: 'container-003'
            paths: [
              '/myPartitionKey'
            ]
            throughput: 500
          }
        ]
        name: 'db-and-container-fixed-throughput-level'
        throughput: 500
      }
      {
        containers: [
          {
            name: 'container-003'
            paths: [
              '/myPartitionKey'
            ]
            throughput: 500
          }
        ]
        name: 'container-fixed-throughput-level'
      }
      {
        containers: [
          {
            name: 'container-003'
            paths: [
              '/myPartitionKey'
            ]
          }
        ]
        name: 'database-fixed-throughput-level'
        throughput: 500
      }
      {
        autoscaleSettingsMaxThroughput: 1000
        containers: [
          {
            autoscaleSettingsMaxThroughput: 1000
            name: 'container-003'
            paths: [
              '/myPartitionKey'
            ]
          }
        ]
        name: 'db-and-container-autoscale-level'
      }
      {
        containers: [
          {
            autoscaleSettingsMaxThroughput: 1000
            name: 'container-003'
            paths: [
              '/myPartitionKey'
            ]
          }
        ]
        name: 'container-autoscale-level'
      }
      {
        autoscaleSettingsMaxThroughput: 1000
        containers: [
          {
            name: 'container-003'
            paths: [
              '/myPartitionKey'
            ]
          }
        ]
        name: 'database-autoscale-level'
      }
      {
        containers: [
          {
            kind: 'MultiHash'
            name: 'container-001'
            paths: [
              '/myPartitionKey1'
              '/myPartitionKey2'
              '/myPartitionKey3'
            ]
          }
          {
            kind: 'MultiHash'
            name: 'container-002'
            paths: [
              'myPartitionKey1'
              'myPartitionKey2'
              'myPartitionKey3'
            ]
          }
          {
            kind: 'Hash'
            name: 'container-003'
            paths: [
              '/myPartitionKey1'
            ]
          }
          {
            kind: 'Hash'
            name: 'container-004'
            paths: [
              'myPartitionKey1'
            ]
          }
          {
            kind: 'Hash'
            name: 'container-005'
            paths: [
              'myPartitionKey1'
            ]
            version: 2
          }
        ]
        name: 'all-partition-key-types'
      }
      {
        containers: []
        name: 'empty-containers-array'
      }
      {
        name: 'no-containers-specified'
      }
    ]
  }
}
