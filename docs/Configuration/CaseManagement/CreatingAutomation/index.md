---
layout: default
title: Creating Automation
nav_order: 25
parent: Case Management
grand_parent: Configuration
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# Creating Automation

The case management module supports basic automation and integration using Notifications and HTTP Endpoint hooks in the
following cases workflow configuration items:

* Cases Workflow Status.
* Cases Workflow Form.
* Cases Workflow Action.
* Cases Workflow Macro.

Navigate Models >> Cases Workflows >> Cases Workflow Status, adding a new entry:

![Image](EmptyStatusCodeConfiguration.png)

Note the check boxes Enable HTTP Endpoint and Notification.

The purpose of both HTTP Endpoints and Notifications is to provide a means to integrate to external system and business
processes within the context of the action being taken in the Case page.

For the purposes of tokenization, the parameters available will be, at a minimum, the values displayed in the default
section of the case:

![Image](CasePayloadExample.png)

Furthermore, the Case Key and Case Key Value will always be embedded in the payload values made available to
Notification or HTTP Endpoint functionality:

![Image](CaseKeyValue.png)

By way of example, upon the invocation of either a Notification or HTTP Endpoint where the above data is available, the
string "AccountID: [@AccountID@]" would be replaced to become "AccountID: Test1".

# HTTP Endpoint Introduction

HTTP Endpoint functionality brings together parameters and makes a POST or GET to a HTTP endpoint. The construction of
the URL fully supports tokenization for the purposes of crafting querystring parameters.

Locate the HTTP Endpoint in the Cases Workflows Status configuration:

![Image](LocationOfHttpEndpointSwitch.png)

Expand on the HTTP Endpoint switch:

![Image](ExpandedHTTPEndpoint.png)

A HTTP Endpoint will upon invocation (in this example the submission of the form that was created in previous training
procedures) compile all values into a JSON document \ message and submit to a remote HTTP endpoint.

In this example the Toptal Post Bin service will be used (https://www.toptal.com/developers/postbin/) and the following
bin has been created:

[https://www.toptal.com/developers/postbin/1665212514812-5780224471818](https://www.toptal.com/developers/postbin/1665212514812-5780224471818)

The above HTTP endpoint will promiscuously accept the posts emitted synchronously from the case management functions.

The HTTP endpoint functionality is intended for the purposes of performing complex integrations and automations for
which a bridge \ middleware must usually be created for that specific purpose.

The page accepts the following parameters for HTTP Endpoint (which is common across all cases workflow entries that
process HTTP endpoints):

| Value              | Description                                                                                                                                                                                                                                 | Example |
|--------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------|
| HTTP Endpoint Type | The type of HTTP endpoint request.  If POST then the values will be compiled in a JSON document and submitted in the POST body.  If GET only the URL will be used in the HTTP request (although keep in mind that the URL is tokenization). | POST    |
| HTTP Endpoint      | https://www.toptal.com/developers/postbin/1665212514812-5780224471818                                                                                                                                                                       | True    |

To start, perform a POST by completing the form as above and below:

![Image](SendingPOSTToToilet.png)

Scroll down the page, click to update to create a version of the Cases Workflows Status:

![Image](VersionOfCasesWorkflowsStatusWithHTTPEndpoint.png)

Navigate to a case:

![Image](CaseReadyToBeGivenNewStatus.png)

Notice the Case Status Bar, specifically the Status:

![Image](LocationOfCaseStatusInCase.png)

Change Case Status to the newly created status:

![Image](ChangeStatus.png)

On selection of the status, there will be no obvious sign that a HTTP Endpoint integration has taken place from the form
submission response, instead for the purposes of evidence the logs will be inspected to understand the interchange
between the HTTP endpoint:

![Image](UpdatedStatus.png)

On refresh of the post bin endpoint however, it can be seen that the data has been posted and received:

![Image](PostBinResult.png)

In the case of GET, the Post body would not be available, instead, data would be transposed to the query. For this
reason, the querystring fully supports tokenization.

Navigate to the Cases Workflow Status:

![Image](CasesWorkflowStatusToBeUpdatedToGet.png)

Locate the HTTP Endpoint type set:

![Image](CurrentHTTPTypeIsPost.png)

Update the HTTP Endpoint Type to GET and alter the HTTP Endpoint to include a resource as follows:

[https://www.toptal.com/developers/postbin/1665212514812-5780224471818?Example=[@AccountId@](https://www.toptal.com/developers/postbin/1665212514812-5780224471818?Example=[@AccountId@])

![Image](UpdatedToGet.png)

Scroll down and click update to create a version of the Cases Workflows Status:

![Image](UpdatedVersionOfCasesWorkflowStatusForHTTPGet.png)

Navigate to a case, changing status back to something else need be:

![Image](CaseReadyForHTTPGet.png)

Change the status to that updated:

![Image](UpdatedCaseForSendingGetHTTP.png)

On inspection of the Post Bin, the GET can be observed with the query string values properly tokenized:

![Image](POSTBinHasGotTokenisedData.png)

Notifications are the sending of Email or SMS messages as a consequence of an event being raised in the Case page. The
notification functionality fully supports Tokenization in the Body, Subject and Destination fields which will replace
tokenized values with the values in the parameters from the triggering event. From the Cases Workflows Status:

![Image](CasesWorkflowStatusReadyForNotification.png)

Locate the switch for Notification:

![Image](LocationOfNotification.png)

Switch Notification to expose properties:

![Image](ExpandedNotificationProperties.png)

Complete a notification using tokenization on the AccountId (keep in mind that all fields support tokenization):

![Image](UpdatedNotificationForEmailTokens.png)

Scroll down and click Update to create a version of the Cases Workflows Status:

![Image](UpdatedVersionForANotification.png)

The process to change to this status is as laid out above, however in addition to a HTTP GET (or POST) being made, an
email (or SMS if that Notification Type has been selected) will be dispatched having been appropriately tokenized:

![Image](EmailRecieved.png)

The Notification Type SMS expects a destination or telephone number, using the + country code designation, and subject
will be ignored.

As both the email and SMS notifications are on the basis of being straight through and synchronous, nothing needs to be
enabled in the engine. However, the following values need to be considered as Environment Variables:

```text
SMTPHost=email.emailhost.com
SMTPPort=587
SMTPUser=UsernameExample
SMTPPassword=PasswordExample
SMTPFrom=richard.churchman@jube.io
```

| Value        | Description                                              |
|--------------|----------------------------------------------------------|
| SMTPHost     | The SMTP host name.                                      |
| SMTPPort     | The SMTP port name.                                      |
| SMTPUser     | The SMTP user name for authentication.                   |
| SMTPPassword | The SMTP password for authentication.                    |
| SMTPFrom     | The email address where the email will be received from. |

Likewise the Clickatell gateway in the case of SMS, using the values in the using a value set Environment Variable as:

```text
ClickatellAPIKey=APIKeyFromClickatell
```

| Value            | Description                         |
|------------------|-------------------------------------|
| ClickatellAPIKey | The API Key provided by Clickatell. |

# POST Body Context

HTTP POST's can be made in the following parts of the case management system:

| Event                | Description                                                                                                  | Context                                                                                                                                                                               |
|----------------------|--------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Case Workflow Action | Creation of a Case Note by the end user alongside a Case Workflow Action.                                    | Case Note including Case Workflow Action name, context of Case including Case Workflow Status Name and context of full Archive payload on case creation.                              |
| Case Workflow Status | On allocation of a status in Activation Rule Case Creation, Elevation or updates being made by the end user. | Case including Case Workflow Status Name and context of full Archive payload on case creation.                                                                                        |
| Case Workflow Macro  | Invocation of the Case Workflow macro by the end user                                                        | Case Workflow Macro definition including Case Workflow Form name, context of Case entry including Case Workflow Status Name and context of full Archive payload on case creation.     |
| Case Workflow Form   | On submission of a Case Workflow Form.                                                                       | Case Workflow Form deinition including Case Workflow Form name and payload, context of Case including Case Workflow Status Name and context of full Archive payload on case creation. |

Case Workflow Action:

```json
{
  "id": 1,
  "note": "Hello",
  "actionId": 3,
  "priorityId": 1,
  "createdDate": "0001-01-01T00:00:00",
  "caseKey": "AccountId",
  "caseKeyValue": "Test20",
  "caseId": 1,
  "caseWorkflowActionName": "Escalation",
  "case": {
    "id": 1,
    "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
    "diaryDate": "2026-02-20T09:19:46.5265",
    "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
    "caseWorkflowStatusGuid": "faf5c83a-5c3e-4a62-b224-aea9e2e1efa6",
    "createdDate": "2026-02-20T09:19:46.526214",
    "locked": 0,
    "closedStatusId": 0,
    "caseKey": "AccountId",
    "diary": 0,
    "rating": 0,
    "caseKeyValue": "Test20",
    "caseWorkflowStatus": "First Line Review",
    "payload": {
      "payload": {
        "IP": "Test",
        "OS": "Lollypop",
        "MAC": "94:23:44f:2:d3",
        "OTP": "466923",
        "Is3D": "False",
        "Brand": "ZTE",
        "Email": "please@hash.me",
        "IsAPM": "True",
        "Model": "Barby",
        "TxnId": "0987654321",
        "BankId": "57",
        "System": "Android",
        "OrderId": "10607324128",
        "Storage": "True",
        "Currency": "826",
        "DeviceId": "OlaRoseGoldPhone3",
        "IsRebill": "False",
        "AccountId": "Test20",
        "AmountEUR": "100",
        "AmountGBP": "86.5866",
        "AmountUSD": "113.055",
        "ChannelId": "1",
        "Jailbreak": "True",
        "ActionDate": "2019-04-17T04:18:15.0000000+03:00",
        "BillingZip": "123456",
        "IsCascaded": "False",
        "IsCredited": "False",
        "IsModified": "False",
        "JoinedName": "Robert Mugabe",
        "Resolution": "720*1280",
        "BillingCity": "Address Line 2",
        "ServiceCode": "DID",
        "ToAccountId": "MTN",
        "TwoFATypeId": "SMS",
        "TxnDateTime": "2024-12-01T21:41:37.2480000+02:00",
        "APMAccountId": "",
        "BillingPhone": "1234567890",
        "BillingState": "",
        "ResponseCode": "0",
        "AmountEURRate": "1",
        "AmountGBPRate": "0.8658658602",
        "AmountUSDRate": "1.1305502954",
        "BusinessModel": "Sports betting",
        "AppVersionCode": "12.34",
        "BillingAddress": "Address Line 1",
        "BillingCountry": "DE",
        "CreditCardHash": "0xDA39A3EE5E6B4B0JJJ1890AFD80709",
        "CurrencyAmount": "123.45",
        "IsModification": "False",
        "OriginalAmount": "100",
        "AccountCurrency": "566",
        "AccountLatitude": "5.3536",
        "BillingLastName": "Mugabe",
        "FingerprintHash": "jhjkhjkhsjh2hjhjkhj2k",
        "TwoFAResponseId": "1",
        "AccountLongitude": "36.1408",
        "AcquirerBankName": "Caixa",
        "BillingFirstName": "Robert",
        "DebuggerAttached": "True",
        "OriginalCurrency": "EUR",
        "SettlementAmount": "100000",
        "SimulatorAttached": "True",
        "TransactionTypeId": "False",
        "IsCurrencyConverted": "False",
        "TransactionResultId": "1006",
        "ToAccountExternalRef": "ChurchmanR",
        "TransactionExternalResponseId": "0"
      },
      "responseElevation": {
        "value": 2.0,
        "redirect": "https://www.jube.io",
        "foreColor": "#fb0707",
        "backColor": "#f5f2cb",
        "content": "Declined for \"VolumeThresholdByAccountId\"",
        "createdDate": "2026-02-20T09:19:46.0658815+02:00"
      },
      "r": 0.0,
      "createdDate": "2026-02-20T09:19:46.0732995+02:00",
      "entityAnalysisModelGuid": "90c425fd-101a-420b-91d1-cb7a24a969cc",
      "entityAnalysisModelName": "Detailed_Account_Financial_Transactions",
      "entityAnalysisModelInstanceGuid": "00000000-0000-0000-0000-000000000000",
      "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
      "entityInstanceEntryId": "0987654321",
      "responseElevationLimit": 10.0,
      "referenceDate": "2024-12-01T21:41:37.248",
      "archiveEnqueueDate": "2026-02-20T09:19:46.4475738+02:00",
      "matchedGatewayRule": true,
      "prevailingEntityAnalysisModelActivationRuleId": 5,
      "entityAnalysisModelActivationRuleCount": 2,
      "dictionary": {
        "VolumeThresholdByAccountId": 0.0
      },
      "ttlCounter": {
        "TtlCounterAll": 0
      },
      "sanction": {
        "FuzzyMatchDistance2JoinedName": 0.0
      },
      "abstraction": {
        "ResponseCodeEqual0Volume": 1111.0500000000002,
        "NotResponseCodeEqual0Volume": 0.0
      },
      "abstractionCalculation": {
        "ResponseCodeVolumeRatio": 0.0
      },
      "httpAdaptation": {},
      "exhaustiveAdaptation": {},
      "activation": {
        "VolumeThresholdByAccountId": {
          "visible": true
        },
        "ThresholdSanctionsDistance": {
          "visible": true
        },
        "IncrementTtlCounterAll": {
          "visible": false
        }
      },
      "createCase": {
        "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
        "caseWorkflowStatusGuid": "faf5c83a-5c3e-4a62-b224-aea9e2e1efa6",
        "caseKeyValue": "Test20",
        "caseKey": "AccountId",
        "suspendBypass": false,
        "suspendBypassDate": "2026-02-20T09:19:46.3793539+02:00"
      },
      "tag": {},
      "invokeTaskPerformance": {
        "computeTimes": {
          "parse": 48677,
          "inlineFunction": 79002,
          "inlineScript": 86319,
          "gateway": 88247,
          "sanctionsAsync": 117393,
          "dictionaryKvPsAsync": 100138,
          "ttlCountersAsync": 117850,
          "abstractionRulesWithSearchKeysAsync": 186838,
          "readTasksPerformance": {
            "sanctionsAsync": {
              "computeTime": 11839960,
              "memory": 27349
            },
            "dictionaryKvPsAsync": {
              "computeTime": 1624,
              "memory": 10131
            },
            "ttlCountersAsync": {
              "computeTime": 2603136,
              "memory": 29327
            },
            "abstractionRulesWithSearchKeysAsync": {
              "computeTime": 83912,
              "memory": 96653
            }
          },
          "joinReadTasks": 188121,
          "executeAbstractionRulesWithoutSearchKey": 189938,
          "executeAbstractionCalculation": 192925,
          "executeExhaustiveAdaptation": 194053,
          "executeHttpAdaptation": 196472,
          "executeActivation": 325799,
          "joinWriteTasks": 327992,
          "writeTasksPerformance": {
            "cachePayloadLatestUpsertAsync": {
              "computeTime": 20680,
              "memory": 34383
            },
            "cachePayloadInsertAsync": {
              "computeTime": 9480,
              "memory": 11739
            },
            "cacheTtlCounterEntryUpsertAsync": {
              "computeTime": 688,
              "memory": 2816
            },
            "cacheTtlCounterEntryIncrementAsync": {
              "computeTime": 656,
              "memory": 2800
            }
          }
        },
        "memory": 8860472
      }
    }
  }
}
```

Case Workflow Macro:

```json
{
  "caseId": 1,
  "caseWorkflowMacroId": 1,
  "caseWorkflowMacroName": "ExampleMacro",
  "case": {
    "id": 1,
    "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
    "diaryDate": "2026-02-20T09:19:46.5265",
    "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
    "caseWorkflowStatusGuid": "faf5c83a-5c3e-4a62-b224-aea9e2e1efa6",
    "createdDate": "2026-02-20T09:19:46.526214",
    "locked": 0,
    "closedStatusId": 0,
    "caseKey": "AccountId",
    "diary": 0,
    "rating": 0,
    "caseKeyValue": "Test20",
    "caseWorkflowStatus": "First Line Review",
    "payload": {
      "payload": {
        "IP": "Test",
        "OS": "Lollypop",
        "MAC": "94:23:44f:2:d3",
        "OTP": "466923",
        "Is3D": "False",
        "Brand": "ZTE",
        "Email": "please@hash.me",
        "IsAPM": "True",
        "Model": "Barby",
        "TxnId": "0987654321",
        "BankId": "57",
        "System": "Android",
        "OrderId": "10607324128",
        "Storage": "True",
        "Currency": "826",
        "DeviceId": "OlaRoseGoldPhone3",
        "IsRebill": "False",
        "AccountId": "Test20",
        "AmountEUR": "100",
        "AmountGBP": "86.5866",
        "AmountUSD": "113.055",
        "ChannelId": "1",
        "Jailbreak": "True",
        "ActionDate": "2019-04-17T04:18:15.0000000+03:00",
        "BillingZip": "123456",
        "IsCascaded": "False",
        "IsCredited": "False",
        "IsModified": "False",
        "JoinedName": "Robert Mugabe",
        "Resolution": "720*1280",
        "BillingCity": "Address Line 2",
        "ServiceCode": "DID",
        "ToAccountId": "MTN",
        "TwoFATypeId": "SMS",
        "TxnDateTime": "2024-12-01T21:41:37.2480000+02:00",
        "APMAccountId": "",
        "BillingPhone": "1234567890",
        "BillingState": "",
        "ResponseCode": "0",
        "AmountEURRate": "1",
        "AmountGBPRate": "0.8658658602",
        "AmountUSDRate": "1.1305502954",
        "BusinessModel": "Sports betting",
        "AppVersionCode": "12.34",
        "BillingAddress": "Address Line 1",
        "BillingCountry": "DE",
        "CreditCardHash": "0xDA39A3EE5E6B4B0JJJ1890AFD80709",
        "CurrencyAmount": "123.45",
        "IsModification": "False",
        "OriginalAmount": "100",
        "AccountCurrency": "566",
        "AccountLatitude": "5.3536",
        "BillingLastName": "Mugabe",
        "FingerprintHash": "jhjkhjkhsjh2hjhjkhj2k",
        "TwoFAResponseId": "1",
        "AccountLongitude": "36.1408",
        "AcquirerBankName": "Caixa",
        "BillingFirstName": "Robert",
        "DebuggerAttached": "True",
        "OriginalCurrency": "EUR",
        "SettlementAmount": "100000",
        "SimulatorAttached": "True",
        "TransactionTypeId": "False",
        "IsCurrencyConverted": "False",
        "TransactionResultId": "1006",
        "ToAccountExternalRef": "ChurchmanR",
        "TransactionExternalResponseId": "0"
      },
      "responseElevation": {
        "value": 2.0,
        "redirect": "https://www.jube.io",
        "foreColor": "#fb0707",
        "backColor": "#f5f2cb",
        "content": "Declined for \"VolumeThresholdByAccountId\"",
        "createdDate": "2026-02-20T09:19:46.0658815+02:00"
      },
      "r": 0.0,
      "createdDate": "2026-02-20T09:19:46.0732995+02:00",
      "entityAnalysisModelGuid": "90c425fd-101a-420b-91d1-cb7a24a969cc",
      "entityAnalysisModelName": "Detailed_Account_Financial_Transactions",
      "entityAnalysisModelInstanceGuid": "00000000-0000-0000-0000-000000000000",
      "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
      "entityInstanceEntryId": "0987654321",
      "responseElevationLimit": 10.0,
      "referenceDate": "2024-12-01T21:41:37.248",
      "archiveEnqueueDate": "2026-02-20T09:19:46.4475738+02:00",
      "matchedGatewayRule": true,
      "prevailingEntityAnalysisModelActivationRuleId": 5,
      "entityAnalysisModelActivationRuleCount": 2,
      "dictionary": {
        "VolumeThresholdByAccountId": 0.0
      },
      "ttlCounter": {
        "TtlCounterAll": 0
      },
      "sanction": {
        "FuzzyMatchDistance2JoinedName": 0.0
      },
      "abstraction": {
        "NotResponseCodeEqual0Volume": 0.0,
        "ResponseCodeEqual0Volume": 1111.0500000000002
      },
      "abstractionCalculation": {
        "ResponseCodeVolumeRatio": 0.0
      },
      "httpAdaptation": {},
      "exhaustiveAdaptation": {},
      "activation": {
        "VolumeThresholdByAccountId": {
          "visible": true
        },
        "ThresholdSanctionsDistance": {
          "visible": true
        },
        "IncrementTtlCounterAll": {
          "visible": false
        }
      },
      "createCase": {
        "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
        "caseWorkflowStatusGuid": "faf5c83a-5c3e-4a62-b224-aea9e2e1efa6",
        "caseKeyValue": "Test20",
        "caseKey": "AccountId",
        "suspendBypass": false,
        "suspendBypassDate": "2026-02-20T09:19:46.3793539+02:00"
      },
      "tag": {},
      "invokeTaskPerformance": {
        "computeTimes": {
          "parse": 48677,
          "inlineFunction": 79002,
          "inlineScript": 86319,
          "gateway": 88247,
          "sanctionsAsync": 117393,
          "dictionaryKvPsAsync": 100138,
          "ttlCountersAsync": 117850,
          "abstractionRulesWithSearchKeysAsync": 186838,
          "readTasksPerformance": {
            "sanctionsAsync": {
              "computeTime": 11839960,
              "memory": 27349
            },
            "dictionaryKvPsAsync": {
              "computeTime": 1624,
              "memory": 10131
            },
            "ttlCountersAsync": {
              "computeTime": 2603136,
              "memory": 29327
            },
            "abstractionRulesWithSearchKeysAsync": {
              "computeTime": 83912,
              "memory": 96653
            }
          },
          "joinReadTasks": 188121,
          "executeAbstractionRulesWithoutSearchKey": 189938,
          "executeAbstractionCalculation": 192925,
          "executeExhaustiveAdaptation": 194053,
          "executeHttpAdaptation": 196472,
          "executeActivation": 325799,
          "joinWriteTasks": 327992,
          "writeTasksPerformance": {
            "cachePayloadLatestUpsertAsync": {
              "computeTime": 20680,
              "memory": 34383
            },
            "cachePayloadInsertAsync": {
              "computeTime": 9480,
              "memory": 11739
            },
            "cacheTtlCounterEntryUpsertAsync": {
              "computeTime": 688,
              "memory": 2816
            },
            "cacheTtlCounterEntryIncrementAsync": {
              "computeTime": 656,
              "memory": 2800
            }
          }
        },
        "memory": 8860472
      }
    }
  }
}
```

Case Workflow Status:

```json
{
  "id": 1,
  "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
  "diaryDate": "2026-02-20T05:19:46Z",
  "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
  "caseWorkflowStatusGuid": "3ff54c93-ad25-46e7-a1aa-9efa488f928e",
  "createdDate": "2026-02-20T09:19:46.526214",
  "locked": 0,
  "lockedUser": "",
  "closedStatusId": 0,
  "caseKey": "AccountId",
  "diary": 0,
  "rating": 1,
  "caseKeyValue": "Test20",
  "caseWorkflowStatusName": "Fraudulent",
  "payload": {
    "payload": {
      "IP": "Test",
      "OS": "Lollypop",
      "MAC": "94:23:44f:2:d3",
      "OTP": "466923",
      "Is3D": "False",
      "Brand": "ZTE",
      "Email": "please@hash.me",
      "IsAPM": "True",
      "Model": "Barby",
      "TxnId": "0987654321",
      "BankId": "57",
      "System": "Android",
      "OrderId": "10607324128",
      "Storage": "True",
      "Currency": "826",
      "DeviceId": "OlaRoseGoldPhone3",
      "IsRebill": "False",
      "AccountId": "Test20",
      "AmountEUR": "100",
      "AmountGBP": "86.5866",
      "AmountUSD": "113.055",
      "ChannelId": "1",
      "Jailbreak": "True",
      "ActionDate": "2019-04-17T04:18:15.0000000+03:00",
      "BillingZip": "123456",
      "IsCascaded": "False",
      "IsCredited": "False",
      "IsModified": "False",
      "JoinedName": "Robert Mugabe",
      "Resolution": "720*1280",
      "BillingCity": "Address Line 2",
      "ServiceCode": "DID",
      "ToAccountId": "MTN",
      "TwoFATypeId": "SMS",
      "TxnDateTime": "2024-12-01T21:41:37.2480000+02:00",
      "APMAccountId": "",
      "BillingPhone": "1234567890",
      "BillingState": "",
      "ResponseCode": "0",
      "AmountEURRate": "1",
      "AmountGBPRate": "0.8658658602",
      "AmountUSDRate": "1.1305502954",
      "BusinessModel": "Sports betting",
      "AppVersionCode": "12.34",
      "BillingAddress": "Address Line 1",
      "BillingCountry": "DE",
      "CreditCardHash": "0xDA39A3EE5E6B4B0JJJ1890AFD80709",
      "CurrencyAmount": "123.45",
      "IsModification": "False",
      "OriginalAmount": "100",
      "AccountCurrency": "566",
      "AccountLatitude": "5.3536",
      "BillingLastName": "Mugabe",
      "FingerprintHash": "jhjkhjkhsjh2hjhjkhj2k",
      "TwoFAResponseId": "1",
      "AccountLongitude": "36.1408",
      "AcquirerBankName": "Caixa",
      "BillingFirstName": "Robert",
      "DebuggerAttached": "True",
      "OriginalCurrency": "EUR",
      "SettlementAmount": "100000",
      "SimulatorAttached": "True",
      "TransactionTypeId": "False",
      "IsCurrencyConverted": "False",
      "TransactionResultId": "1006",
      "ToAccountExternalRef": "ChurchmanR",
      "TransactionExternalResponseId": "0"
    },
    "responseElevation": {
      "value": 2.0,
      "redirect": "https://www.jube.io",
      "foreColor": "#fb0707",
      "backColor": "#f5f2cb",
      "content": "Declined for \"VolumeThresholdByAccountId\"",
      "createdDate": "2026-02-20T09:19:46.0658815+02:00"
    },
    "r": 0.0,
    "createdDate": "2026-02-20T09:19:46.0732995+02:00",
    "entityAnalysisModelGuid": "90c425fd-101a-420b-91d1-cb7a24a969cc",
    "entityAnalysisModelName": "Detailed_Account_Financial_Transactions",
    "entityAnalysisModelInstanceGuid": "00000000-0000-0000-0000-000000000000",
    "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
    "entityInstanceEntryId": "0987654321",
    "responseElevationLimit": 10.0,
    "referenceDate": "2024-12-01T21:41:37.248",
    "archiveEnqueueDate": "2026-02-20T09:19:46.4475738+02:00",
    "matchedGatewayRule": true,
    "prevailingEntityAnalysisModelActivationRuleId": 5,
    "entityAnalysisModelActivationRuleCount": 2,
    "dictionary": {
      "VolumeThresholdByAccountId": 0.0
    },
    "ttlCounter": {
      "TtlCounterAll": 0
    },
    "sanction": {
      "FuzzyMatchDistance2JoinedName": 0.0
    },
    "abstraction": {
      "ResponseCodeEqual0Volume": 1111.0500000000002,
      "NotResponseCodeEqual0Volume": 0.0
    },
    "abstractionCalculation": {
      "ResponseCodeVolumeRatio": 0.0
    },
    "httpAdaptation": {},
    "exhaustiveAdaptation": {},
    "activation": {
      "ThresholdSanctionsDistance": {
        "visible": true
      },
      "VolumeThresholdByAccountId": {
        "visible": true
      },
      "IncrementTtlCounterAll": {
        "visible": false
      }
    },
    "createCase": {
      "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
      "caseWorkflowStatusGuid": "faf5c83a-5c3e-4a62-b224-aea9e2e1efa6",
      "caseKeyValue": "Test20",
      "caseKey": "AccountId",
      "suspendBypass": false,
      "suspendBypassDate": "2026-02-20T09:19:46.3793539+02:00"
    },
    "tag": {},
    "invokeTaskPerformance": {
      "computeTimes": {
        "parse": 48677,
        "inlineFunction": 79002,
        "inlineScript": 86319,
        "gateway": 88247,
        "sanctionsAsync": 117393,
        "dictionaryKvPsAsync": 100138,
        "ttlCountersAsync": 117850,
        "abstractionRulesWithSearchKeysAsync": 186838,
        "readTasksPerformance": {
          "sanctionsAsync": {
            "computeTime": 11839960,
            "memory": 27349
          },
          "dictionaryKvPsAsync": {
            "computeTime": 1624,
            "memory": 10131
          },
          "ttlCountersAsync": {
            "computeTime": 2603136,
            "memory": 29327
          },
          "abstractionRulesWithSearchKeysAsync": {
            "computeTime": 83912,
            "memory": 96653
          }
        },
        "joinReadTasks": 188121,
        "executeAbstractionRulesWithoutSearchKey": 189938,
        "executeAbstractionCalculation": 192925,
        "executeExhaustiveAdaptation": 194053,
        "executeHttpAdaptation": 196472,
        "executeActivation": 325799,
        "joinWriteTasks": 327992,
        "writeTasksPerformance": {
          "cachePayloadLatestUpsertAsync": {
            "computeTime": 20680,
            "memory": 34383
          },
          "cachePayloadInsertAsync": {
            "computeTime": 9480,
            "memory": 11739
          },
          "cacheTtlCounterEntryUpsertAsync": {
            "computeTime": 688,
            "memory": 2816
          },
          "cacheTtlCounterEntryIncrementAsync": {
            "computeTime": 656,
            "memory": 2800
          }
        }
      },
      "memory": 8860472
    }
  }
}
```

Case Workflow Form:

```json
{
  "id" : 11,
  "createdDate" : "2026-02-20T15:57:53.703344+02:00",
  "createdUser" : "Administrator",
  "caseKey" : "AccountId",
  "caseId" : 1,
  "caseWorkflowFormId" : 1,
  "caseKeyValue" : "Test20",
  "payload" : {
    "ExampleTextboxElement" : "Test"
  },
  "caseWorkflowFormName" : "ExampleForm",
  "case" : {
    "id" : 1,
    "entityAnalysisModelInstanceEntryGuid" : "909c1cac-de56-4f02-ab55-f514df3c1f6d",
    "diaryDate" : "2026-02-20T07:19:46",
    "caseWorkflowGuid" : "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
    "caseWorkflowStatusGuid" : "3ff54c93-ad25-46e7-a1aa-9efa488f928e",
    "createdDate" : "2026-02-20T09:19:46.526214",
    "locked" : 0,
    "lockedUser" : "",
    "closedStatusId" : 0,
    "caseKey" : "AccountId",
    "diary" : 0,
    "rating" : 1,
    "caseKeyValue" : "Test20",
    "caseWorkflowStatus" : "Fraudulent",
    "payload" : {
      "payload" : {
        "IP" : "Test",
        "OS" : "Lollypop",
        "MAC" : "94:23:44f:2:d3",
        "OTP" : "466923",
        "Is3D" : "False",
        "Brand" : "ZTE",
        "Email" : "please@hash.me",
        "IsAPM" : "True",
        "Model" : "Barby",
        "TxnId" : "0987654321",
        "BankId" : "57",
        "System" : "Android",
        "OrderId" : "10607324128",
        "Storage" : "True",
        "Currency" : "826",
        "DeviceId" : "OlaRoseGoldPhone3",
        "IsRebill" : "False",
        "AccountId" : "Test20",
        "AmountEUR" : "100",
        "AmountGBP" : "86.5866",
        "AmountUSD" : "113.055",
        "ChannelId" : "1",
        "Jailbreak" : "True",
        "ActionDate" : "2019-04-17T04:18:15.0000000+03:00",
        "BillingZip" : "123456",
        "IsCascaded" : "False",
        "IsCredited" : "False",
        "IsModified" : "False",
        "JoinedName" : "Robert Mugabe",
        "Resolution" : "720*1280",
        "BillingCity" : "Address Line 2",
        "ServiceCode" : "DID",
        "ToAccountId" : "MTN",
        "TwoFATypeId" : "SMS",
        "TxnDateTime" : "2024-12-01T21:41:37.2480000+02:00",
        "APMAccountId" : "",
        "BillingPhone" : "1234567890",
        "BillingState" : "",
        "ResponseCode" : "0",
        "AmountEURRate" : "1",
        "AmountGBPRate" : "0.8658658602",
        "AmountUSDRate" : "1.1305502954",
        "BusinessModel" : "Sports betting",
        "AppVersionCode" : "12.34",
        "BillingAddress" : "Address Line 1",
        "BillingCountry" : "DE",
        "CreditCardHash" : "0xDA39A3EE5E6B4B0JJJ1890AFD80709",
        "CurrencyAmount" : "123.45",
        "IsModification" : "False",
        "OriginalAmount" : "100",
        "AccountCurrency" : "566",
        "AccountLatitude" : "5.3536",
        "BillingLastName" : "Mugabe",
        "FingerprintHash" : "jhjkhjkhsjh2hjhjkhj2k",
        "TwoFAResponseId" : "1",
        "AccountLongitude" : "36.1408",
        "AcquirerBankName" : "Caixa",
        "BillingFirstName" : "Robert",
        "DebuggerAttached" : "True",
        "OriginalCurrency" : "EUR",
        "SettlementAmount" : "100000",
        "SimulatorAttached" : "True",
        "TransactionTypeId" : "False",
        "IsCurrencyConverted" : "False",
        "TransactionResultId" : "1006",
        "ToAccountExternalRef" : "ChurchmanR",
        "TransactionExternalResponseId" : "0"
      },
      "responseElevation" : {
        "value" : 2.0,
        "redirect" : "https://www.jube.io",
        "foreColor" : "#fb0707",
        "backColor" : "#f5f2cb",
        "content" : "Declined for \"VolumeThresholdByAccountId\"",
        "createdDate" : "2026-02-20T09:19:46.0658815+02:00"
      },
      "r" : 0.0,
      "createdDate" : "2026-02-20T09:19:46.0732995+02:00",
      "entityAnalysisModelGuid" : "90c425fd-101a-420b-91d1-cb7a24a969cc",
      "entityAnalysisModelName" : "Detailed_Account_Financial_Transactions",
      "entityAnalysisModelInstanceGuid" : "00000000-0000-0000-0000-000000000000",
      "entityAnalysisModelInstanceEntryGuid" : "909c1cac-de56-4f02-ab55-f514df3c1f6d",
      "entityInstanceEntryId" : "0987654321",
      "responseElevationLimit" : 10.0,
      "referenceDate" : "2024-12-01T21:41:37.248",
      "archiveEnqueueDate" : "2026-02-20T09:19:46.4475738+02:00",
      "matchedGatewayRule" : true,
      "prevailingEntityAnalysisModelActivationRuleId" : 5,
      "entityAnalysisModelActivationRuleCount" : 2,
      "dictionary" : {
        "VolumeThresholdByAccountId" : 0.0
      },
      "ttlCounter" : {
        "TtlCounterAll" : 0
      },
      "sanction" : {
        "FuzzyMatchDistance2JoinedName" : 0.0
      },
      "abstraction" : {
        "ResponseCodeEqual0Volume" : 1111.0500000000002,
        "NotResponseCodeEqual0Volume" : 0.0
      },
      "abstractionCalculation" : {
        "ResponseCodeVolumeRatio" : 0.0
      },
      "httpAdaptation" : { },
      "exhaustiveAdaptation" : { },
      "activation" : {
        "IncrementTtlCounterAll" : {
          "visible" : false
        },
        "VolumeThresholdByAccountId" : {
          "visible" : true
        },
        "ThresholdSanctionsDistance" : {
          "visible" : true
        }
      },
      "createCase" : {
        "caseWorkflowGuid" : "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
        "caseWorkflowStatusGuid" : "faf5c83a-5c3e-4a62-b224-aea9e2e1efa6",
        "caseKeyValue" : "Test20",
        "caseKey" : "AccountId",
        "suspendBypass" : false,
        "suspendBypassDate" : "2026-02-20T09:19:46.3793539+02:00"
      },
      "tag" : { },
      "invokeTaskPerformance" : {
        "computeTimes" : {
          "parse" : 48677,
          "inlineFunction" : 79002,
          "inlineScript" : 86319,
          "gateway" : 88247,
          "sanctionsAsync" : 117393,
          "dictionaryKvPsAsync" : 100138,
          "ttlCountersAsync" : 117850,
          "abstractionRulesWithSearchKeysAsync" : 186838,
          "readTasksPerformance" : {
            "sanctionsAsync" : {
              "computeTime" : 11839960,
              "memory" : 27349
            },
            "dictionaryKvPsAsync" : {
              "computeTime" : 1624,
              "memory" : 10131
            },
            "ttlCountersAsync" : {
              "computeTime" : 2603136,
              "memory" : 29327
            },
            "abstractionRulesWithSearchKeysAsync" : {
              "computeTime" : 83912,
              "memory" : 96653
            }
          },
          "joinReadTasks" : 188121,
          "executeAbstractionRulesWithoutSearchKey" : 189938,
          "executeAbstractionCalculation" : 192925,
          "executeExhaustiveAdaptation" : 194053,
          "executeHttpAdaptation" : 196472,
          "executeActivation" : 325799,
          "joinWriteTasks" : 327992,
          "writeTasksPerformance" : {
            "cachePayloadLatestUpsertAsync" : {
              "computeTime" : 20680,
              "memory" : 34383
            },
            "cachePayloadInsertAsync" : {
              "computeTime" : 9480,
              "memory" : 11739
            },
            "cacheTtlCounterEntryUpsertAsync" : {
              "computeTime" : 688,
              "memory" : 2816
            },
            "cacheTtlCounterEntryIncrementAsync" : {
              "computeTime" : 656,
              "memory" : 2800
            }
          }
        },
        "memory" : 8860472
      }
    }
  }
}
```