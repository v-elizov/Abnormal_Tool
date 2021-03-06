﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Abnormal_UI.UI.Samr;
using MongoDB.Bson;

namespace Abnormal_UI.Infra
{
    public static class DocumentCreator
    {
        private static readonly Random Random = new Random();

        public static BsonDocument KerberosCreator(EntityObject userEntity, EntityObject computerEntity,
            EntityObject domainController, string userDomainName,string computerDomainName, ObjectId sourceGateway, string targetSpn = null,
            EntityObject targetMachine = null, string actionType = "As", int daysToSubtruct = 0,
            int hoursToSubtract = 0,
            ObjectId parentId = new ObjectId())
        {
            var kerberosTime = DateTime.UtcNow.Subtract(new TimeSpan(daysToSubtruct, hoursToSubtract, 0, 5, 0));
            var sourceAccount = new BsonDocument {{"DomainName", userDomainName}, {"Name", userEntity.Name}};
            var sourceComputerName = new BsonDocument {{"DomainName", computerDomainName }, {"Name", computerEntity.Name}};
            var resourceIdentifier = new BsonDocument();
            var targetSpnName = $"krbtgt/{userDomainName}";
            if (targetSpn != null)
            {
                targetSpnName = targetSpn;
            }
            resourceIdentifier.Add("AccountId", targetMachine?.Id ?? domainController.Id);
            var resourceName = new BsonDocument {{"DomainName", userDomainName}, {"Name", targetSpnName}};
            resourceIdentifier.Add("ResourceName", resourceName);
            var destinationComputerName =
                new BsonDocument {{"DomainName", userDomainName}, {"Name", domainController.Name}};
            var responseTicket = new BsonDocument
            {
                {"EncryptionType", "Aes256CtsHmacSha196"},
                {"IsReferral", false},
                {"Realm", userDomainName},
                {"ResourceIdentifier", resourceIdentifier},
                {"Size", 1084},
                {"Hash", new byte[16]}
            };
            var networkActivityDocument = new BsonDocument
            {
                {"_id", ObjectId.GenerateNewId()},
                {
                    "_t",
                    new BsonArray(new[]
                        {"Entity", "NetworkActivity", "Kerberos", "KerberosKdc", "Kerberos" + actionType})
                },
                {"HorizontalParentId", ObjectId.GenerateNewId()},
                {"StartTime", kerberosTime},
                {"EndTime", kerberosTime},
                {"SourceIpAddress", "[daf::daf]"},
                {"SourcePort", 38014},
                {"SourceComputerId", computerEntity.Id},
                {"SourceComputerCertainty", "High"},
                {"SourceComputerResolutionMethod", new BsonArray(new[] {"RpcNtlm"})},
                {"DestinationIpAddress", "[daf::200]"},
                {"DestinationComputerId", domainController.Id},
                {"DestinationComputerCertainty", "High"},
                {"DestinationComputerResolutionMethod", new BsonArray(new[] {"RpcNtlm"})},
                {"DestinationComputerName", destinationComputerName},
                {"TransportProtocol", "Tcp"},
                {"DomainControllerStartTime", kerberosTime},
                {"SourceAccountName", sourceAccount},
                {"SourceAccountId", userEntity.Id},
                {"ResourceIdentifier", resourceIdentifier},
                {"SourceComputerName", sourceComputerName},
                {"Error", "Success"},
                {"NtStatus", BsonValue.Create(null)},
                {"SourceGatewaySystemProfileId", sourceGateway},
                {"RequestTicketKerberosId", parentId},
                {"SourceAccountBadPasswordTime", BsonValue.Create(null)},
                {"IsOldPassword", BsonValue.Create(null)},
                {"IsSuccess", BsonValue.Create(true)}
            };
            switch (actionType)
            {
                case "As":

                    networkActivityDocument.Add("SourceComputerNetbiosName", computerEntity.Name);
                    networkActivityDocument.Add("IsIncludePac", BsonValue.Create(true));
                    networkActivityDocument.Add("SourceAccountSupportedEncryptionTypes", new BsonArray(new string[0]));
                    networkActivityDocument.Add("EncryptedTimestampEncryptionType", BsonValue.Create(null));
                    networkActivityDocument.Add("EncryptedTimestamp", BsonValue.Create(null));
                    networkActivityDocument.Add("RequestTicket", BsonValue.Create(null));
                    networkActivityDocument.Add("ResponseTicket", responseTicket);
                    networkActivityDocument.Add("IsSmartcardRequiredRc4", BsonValue.Create(false));
                    networkActivityDocument.Add("ArmoringEncryptionType", BsonValue.Create(null));
                    networkActivityDocument.Add("RequestedTicketExpiration", DateTime.UtcNow);
                    networkActivityDocument.Add("SourceComputerSupportedEncryptionTypes",
                        new BsonArray(new[] {"Rc4Hmac"}));
                    networkActivityDocument.Add("DestinationPort", "88");
                    networkActivityDocument.Add("Options",
                        new BsonArray(new[] {"RenewableOk", "Canonicalize", "Renewable", "Forwardable"}));
                    break;
                case "Tgs":
                    networkActivityDocument.Add("IsServiceForUserToSelf", BsonValue.Create(false));
                    networkActivityDocument.Add("IsUserToUser", BsonValue.Create(false));
                    networkActivityDocument.Add("AuthorizationDataSize", BsonValue.Create(null));
                    networkActivityDocument.Add("AuthorizationDataEncryptionType", BsonValue.Create(null));
                    networkActivityDocument.Add("ParentsOptions", "None");
                    networkActivityDocument.Add("AdditionalTickets", new BsonArray(new string[0]));
                    networkActivityDocument.Add("RequestTicket", responseTicket);
                    networkActivityDocument.Add("ResponseTicket", responseTicket);
                    networkActivityDocument.Add("ArmoringEncryptionType", BsonValue.Create(null));
                    networkActivityDocument.Add("RequestedTicketExpiration", DateTime.UtcNow);
                    networkActivityDocument.Add("SourceComputerSupportedEncryptionTypes",
                        new BsonArray(new[] {"Rc4Hmac"}));
                    networkActivityDocument.Add("DestinationPort", "88");
                    networkActivityDocument.Add("Options",
                        new BsonArray(new[] {"RenewableOk", "Canonicalize", "Renewable", "Forwardable"}));
                    break;
                default:
                    networkActivityDocument.Set(1,
                        new BsonArray(new[] {"Entity", "Activity", "NetworkActivity", "Kerberos", "KerberosAp"}));
                    networkActivityDocument.Add("RequestTicket", responseTicket);
                    networkActivityDocument.Add("DestinationPort", "445");
                    networkActivityDocument.Add("Options", "MutualRequired");
                    break;
            }
            return networkActivityDocument;
        }


        public static BsonDocument SimpleBindCreator(EntityObject userEntity, EntityObject computerEntity,
            EntityObject domainControllerName, string userDomainName, string computerDomainName, ObjectId sourceGateway, int daysToSubtruct = 0)
        {
            var oldTime = DateTime.UtcNow.Subtract(new TimeSpan(daysToSubtruct, 0, 0, 0, 0));
            var sourceAccount = new BsonDocument {{"DomainName", userDomainName }, {"Name", userEntity.Name}};
            var sourceComputerName = new BsonDocument {{"DomainName", computerDomainName }, {"Name", computerEntity.Name}};
            var resourceIdentifier = new BsonDocument {{"AccountId", domainControllerName.Id}};
            var targetSpnName = $"krbtgt/{userDomainName}";
            var resourceName = new BsonDocument {{"DomainName", userDomainName }, {"Name", targetSpnName}};
            resourceIdentifier.Add("ResourceName", resourceName);
            var destinationComputerName = new BsonDocument
            {
                {"DomainName", userDomainName},
                {"Name", domainControllerName.Name}
            };
            var networkActivityDocument = new BsonDocument
            {
                {"_id", ObjectId.GenerateNewId()},
                {"_t", new BsonArray(new[] {"Entity", "Activity", "NetworkActivity", "Ldap", "LdapBind"})},
                {"StartTime", oldTime},
                {"EndTime", oldTime},
                {"HorizontalParentId", ObjectId.GenerateNewId()},
                {"SourceIpAddress", "[daf::daf]"},
                {"SourcePort", 6666},
                {"SourceComputerId", computerEntity.Id},
                {"SourceComputerName", sourceComputerName},
                {"SourceComputerCertainty", "High"},
                {"SourceComputerResolutionMethod", new BsonArray(new[] {"RpcNtlm"})},
                {"DestinationIpAddress", "[daf::daf]"},
                {"DestinationPort", 389},
                {"DestinationComputerId", domainControllerName.Id},
                {"DestinationComputerCertainty", "High"},
                {"DestinationComputerName", destinationComputerName},
                {"DestinationComputerResolutionMethod", new BsonArray(new[] {"RpcNtlm"})},
                {"DomainControllerStartTime", oldTime},
                {"TransportProtocol", "Tcp"},
                {"AuthenticationType", "Simple"},
                {"SourceAccountName", sourceAccount},
                {"SourceAccountId", userEntity.Id},
                {
                    "SourceAccountPasswordHash",
                    "9apJEBIqcse0SNKVm4WzIxaHoLkUareMCDlAzhADI72CLmxOMmA8WzicPPI84xxlewEEIqZZOJJ1VqpE5VJT4g=="
                },
                {"ResultCode", "Success"},
                {"IsSuccess", BsonValue.Create(true)},
                {"SourceGatewaySystemProfileId", sourceGateway},
                {"SourceAccountBadPasswordTime", BsonValue.Create(null)},
                {"IsOldPassword", BsonValue.Create(null)},
                {"ResourceIdentifier", resourceIdentifier}
            };
            return networkActivityDocument;
        }

        public static BsonDocument SaFillerSeac(List<EntityObject> userEntity, List<EntityObject> computerEntity,
            Random rnd)
        {
            var detailRecord = new BsonDocument
            {
                {"StartTime", DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0, 0))},
                {"EndTime", DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0, 0))},
                {"SourceComputerId", computerEntity[rnd.Next(0, computerEntity.Count)].Id},
                {
                    "SourceAccountIds", new BsonArray(new[]
                    {
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id
                    })
                },
                {
                    "DestinationComputerIds", new BsonArray(new[]
                    {
                        computerEntity[rnd.Next(0, computerEntity.Count)].Id
                    })
                }
            };
            var suspicousActivityDocument = new BsonDocument
            {
                {"_id", ObjectId.GenerateNewId()},
                {
                    "_t", new BsonArray(new[]
                    {
                        "Entity", "Alert", "SuspiciousActivity", "SuspiciousActivity`1",
                        "LdapSimpleBindCleartextPasswordSuspiciousActivity"
                    })
                },
                {"StartTime", DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0, 0))},
                {"EndTime", DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0, 0))},
                {"IsVisible", BsonValue.Create(true)},
                {"Severity", "Low"},
                {"Status", "Open"},
                {"TitleKey", "LdapSimpleBindCleartextPasswordSuspiciousActivityTitleService"},
                {"TitleDetailKeys", new BsonArray(new string[] { })},
                {
                    "DescriptionFormatKey",
                    "LdapSimpleBindCleartextPasswordSuspiciousActivityDescriptionServiceSourceAccounts"
                },
                {"DescriptionDetailFormatKeys", new BsonArray(new string[] { })},
                {"SystemUpdateTime", DateTime.UtcNow},
                {"HasDetails", BsonValue.Create(true)},
                {"HasInput", BsonValue.Create(false)},
                {"InputTitleKey", "LdapSimpleBindCleartextPasswordSuspiciousActivityInputTitle"},
                {"IsInputProvided", BsonValue.Create(false)},
                {"Note", BsonValue.Create(null)},
                {"RelatedActivityCount", 127},
                {
                    "RelatedUniqueEntityIds", new BsonArray(new[]
                    {
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id,
                        userEntity[rnd.Next(0, userEntity.Count)].Id, userEntity[rnd.Next(0, userEntity.Count)].Id
                    })
                },
                {"DetailsRecords", new BsonArray(new[] {detailRecord})},
                {"Scope", "Service"}
            };
            return suspicousActivityDocument;
        }

        public static BsonDocument SaFillerAe(List<EntityObject> userEntity, List<EntityObject> computerEntity,
            EntityObject domainController, string domainName)
        {
            var records = new List<BsonDocument>();
            var detailRecord = new BsonDocument();
            for (var i = 0; i < 100000; i++)
            {
                detailRecord.Add("DomainName", domainName);
                detailRecord.Add("Name", "ABCDEFGHIJKLMNOP" + i);
                records.Add(detailRecord);
                detailRecord = new BsonDocument();
            }
            var failedSourceAccountNames = new BsonArray(records);
            var suspicousActivityDocument = new BsonDocument
            {
                {"_id", ObjectId.GenerateNewId()},
                {
                    "_t", new BsonArray(new[]
                    {
                        "Entity", "Alert", "SuspiciousActivity", "AccountEnumerationSuspiciousActivity"
                    })
                },
                {"StartTime", DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0, 0))},
                {"EndTime", DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0, 0))},
                {"IsVisible", BsonValue.Create(true)},
                {"Severity", "Medium"},
                {"Status", "Open"},
                {"TitleKey", "AccountEnumerationSuspiciousActivityTitle"},
                {"TitleDetailKeys", new BsonArray(new string[] { })},
                {"DescriptionFormatKey", "AccountEnumerationSuspiciousActivityDescription"},
                {"DescriptionDetailFormatKeys", new BsonArray(new string[] { })},
                {"SystemUpdateTime", DateTime.UtcNow},
                {"HasDetails", BsonValue.Create(false)},
                {"HasInput", BsonValue.Create(false)},
                {"InputTitleKey", "AccountEnumerationSuspiciousActivityInputTitle"},
                {"IsInputProvided", BsonValue.Create(false)},
                {"Note", BsonValue.Create(null)},
                {"RelatedActivityCount", 127},
                {
                    "RelatedUniqueEntityIds", new BsonArray(new[]
                    {
                        computerEntity[2].Id, domainController.Id, userEntity[0].Id, userEntity[1].Id, userEntity[2].Id,
                        userEntity[3].Id, userEntity[4].Id,
                        userEntity[5].Id, userEntity[6].Id
                    })
                },
                {"SourceComputerId", computerEntity[2].Id},
                {"DestinationComputerIds", new BsonArray(new[] {domainController.Id})},
                {"FailedSourceAccountNames", failedSourceAccountNames},
                {
                    "SuccessSourceAccountIds", new BsonArray(new[]
                    {
                        userEntity[0].Id, userEntity[1].Id, userEntity[2].Id, userEntity[3].Id, userEntity[4].Id,
                        userEntity[5].Id, userEntity[6].Id
                    })
                }
            };
            return suspicousActivityDocument;
        }

        public static BsonDocument EventCreator(EntityObject userEntity, EntityObject computerEntity,
            EntityObject domainControllerName, string userDomainName, string computerDomainName, ObjectId sourceGateway, int daysToSubtruct = 0)
        {
            var sourceComputer = new BsonDocument
            {
                {"DomainName", computerDomainName},
                {"Name", computerEntity.Name}
            };
            var sourceAccount = new BsonDocument {{"DomainName", userDomainName }, {"Name", userEntity.Name}};
            var destinationComputer = new BsonDocument
            {
                {"DomainName", BsonValue.Create(null)},
                {"Name", domainControllerName.Name}
            };
            var resourceIdentifier = new BsonDocument();
            var targetSpnName = $"krbtgt/{userDomainName}";
            resourceIdentifier.Add("AccountId", domainControllerName.Id);
            var resourceName = new BsonDocument {{"DomainName", userDomainName }, {"Name", targetSpnName}};
            resourceIdentifier.Add("ResourceName", resourceName);
            var eventActivityDocument = new BsonDocument
            {
                {"_id", ObjectId.GenerateNewId()},
                {
                    "_t",
                    new BsonArray(new[] {"Entity", "Activity", "EventActivity", "WindowsEvent", "NtlmEvent"})
                },
                {"SourceGatewaySystemProfileId", sourceGateway},
                {"SourceComputerId", computerEntity.Id},
                {"DomainControllerId", domainControllerName.Id},
                {"Time", DateTime.UtcNow.Subtract(new TimeSpan(daysToSubtruct, 4, 0, 0, 0))},
                {"SourceComputerName", sourceComputer},
                {"DomainControllerName", destinationComputer},
                {"IsTimeMillisecondsAccurate", BsonValue.Create(true)},
                {"CategoryName", "Security"},
                {"ProviderName", "Microsoft-Windows-Security-Auditing"},
                {"SourceAccountName", sourceAccount},
                {"SourceAccountId", userEntity.Id},
                {"SourceAccountBadPasswordTime", BsonValue.Create(null)},
                {"ErrorCode", "Success"},
                {"IsOldPassword", BsonValue.Create(null)},
                {"IsSuccess", BsonValue.Create(true)},
                {"ResourceIdentifier", resourceIdentifier}
            };
            return eventActivityDocument;
        }

        public static BsonDocument NtlmCreator(EntityObject userEntity, EntityObject computerEntity,
            EntityObject domainController, string userDomainName, string computerDomainName, ObjectId sourceGateway, EntityObject targetMachine = null,
            int daysToSubtruct = 0, int hoursToSubtract = 0)
        {
            var oldTime = DateTime.UtcNow.Subtract(new TimeSpan(daysToSubtruct, hoursToSubtract, 0, 0, 0));
            var sourceAccount = new BsonDocument {{"DomainName", userDomainName }, {"Name", userEntity.Name}};
            var sourceComputerName = new BsonDocument {{"DomainName", computerDomainName }, {"Name", computerEntity.Name}};
            var resourceIdentifier = new BsonDocument();
            var targetAccount = targetMachine ?? domainController;
            var targetSpnName = $"krbtgt/{userDomainName}";
            resourceIdentifier.Add("AccountId", targetAccount.Id);
            var resourceName = new BsonDocument {{"DomainName", userDomainName }, {"Name", targetSpnName}};
            resourceIdentifier.Add("ResourceName", resourceName);
            var destinationComputerName = new BsonDocument {{"DomainName", userDomainName }, {"Name", targetAccount.Name}};
            var networkActivityDocument = new BsonDocument
            {
                {"_id", ObjectId.GenerateNewId()},
                {"_t", new BsonArray(new[] {"Entity", "NetworkActivity", "Ntlm"})},
                {"SourceGatewaySystemProfileId", sourceGateway},
                {"SourceComputerId", computerEntity.Id},
                {"DestinationComputerId", targetAccount.Id},
                {"HorizontalParentId", ObjectId.GenerateNewId()},
                {"StartTime", oldTime},
                {"EndTime", oldTime},
                {"SourceIpAddress", "[daf::daf]"},
                {"SourcePort", 51510},
                {"SourceComputerCertainty", "High"},
                {"SourceComputerResolutionMethod", new BsonArray(new[] {"RpcNtlm"})},
                {"DestinationIpAddress", "[daf::daf]"},
                {"DestinationPort", 445},
                {"DestinationComputerCertainty", "High"},
                {"DestinationComputerResolutionMethod", new BsonArray(new[] {"RpcNtlm"})},
                {"DestinationComputerName", destinationComputerName},
                {"DomainControllerStartTime", oldTime},
                {"TransportProtocol", "Tcp"},
                {"Version", 2},
                {"SourceComputerName", sourceComputerName},
                {"SourceComputerNetbiosName", computerEntity.Name},
                {"SourceAccountName", sourceAccount},
                {"SourceAccountId", userEntity.Id},
                {"SourceAccountBadPasswordTime", BsonValue.Create(null)},
                {"ResourceIdentifier", resourceIdentifier},
                {"DceRpcStatus", BsonValue.Create(null)},
                {"LdapResultCode", BsonValue.Create(null)},
                {"SmbStatus", "Success"},
                {"Smb1Status", BsonValue.Create(null)},
                {"IsOldPassword", BsonValue.Create(null)},
                {"IsSuccess", BsonValue.Create(true)}
            };
            return networkActivityDocument;
        }

        public static BsonDocument VpnEventCreator(EntityObject userEntity, EntityObject computerEntity,
            EntityObject domainController, string userDomainName, string computerDomainName, ObjectId sourceGateway, string externalSourceIp)
        {
            var sourceComputer = new BsonDocument
            {
                {"DomainName", computerDomainName},
                {"Name", computerEntity.Name}
            };
            var serverName = new BsonDocument
            {
                {"DomainName", BsonValue.Create(null)},
                {"Name", "MS-VPN"}
            };
            var sourceAccount = new BsonDocument {{"DomainName", userDomainName }, {"Name", userEntity.Name}};
            return new BsonDocument
            {
                {"_id", ObjectId.GenerateNewId()},
                {
                    "_t",
                    new BsonArray(new[] {"Entity", "Activity", "EventActivity", "VpnAuthenticationEvent"})
                },
                {"SourceGatewaySystemProfileId", sourceGateway},
                {"Time", DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0, 0))},
                {"IsTimeMillisecondsAccurate", BsonValue.Create(false)},
                {"EventType", "Connect"},
                {"SessionId", "1"},
                {"SourceAccountName", sourceAccount},
                {"SourceAccountId", userEntity.Id},
                {"SourceComputerName", sourceComputer},
                {"SourceComputerId", computerEntity.Id},
                {"ExternalSourceIpAddress", externalSourceIp},
                {"InternalSourceIpAddress", BsonValue.Create(null)},
                {"ServerName", serverName},
                {"ServerInternalIpAddress", "192.168.0.200"},
                {"SourceGeolocationId", BsonValue.Create(null)},
                {"SourceGeolocationConfidenceLevel", "None"},
                {"Carrier", BsonValue.Create(null)},
                {"ServerId", domainController.Id}
            };
        }

        public static BsonDocument SamrCreator(EntityObject userEntity, EntityObject computerEntity,
            EntityObject domainController, string userDomainName, string computerDomainName, ObjectId sourceGateway, bool sensitive,
            SamrViewModel.SamrQueryType queryType, SamrViewModel.SamrQueryOperation queryOperation, string domainId,
            int daysToSubtruct = 0, EntityObject queriedEntityObject = null)
        {
            var dateTime = DateTime.UtcNow.Subtract(new TimeSpan(daysToSubtruct, 0, 0, 0, 0));
            var sourceComputerName =
                new BsonDocument {{"DomainName", computerDomainName}, {"Name", computerEntity.Name}};
            var destinationComputerName =
                new BsonDocument {{"DomainName", userDomainName }, {"Name", domainController.Name}};
            var networkActivityDocument = new BsonDocument
            {
                {"_id", ObjectId.GenerateNewId()},
                {"_t", new BsonArray(new[] {"Entity", "Activity", "NetworkActivity", "Samr", queryType.ToString()})},
                {"SourceGatewaySystemProfileId", sourceGateway},
                {"HorizontalParentId", ObjectId.GenerateNewId()},
                {"StartTime", dateTime},
                {"EndTime", dateTime},
                {"SourceIpAddress", "[daf::daf]"},
                {"SourcePort", 38014},
                {"SourceComputerName", sourceComputerName},
                {"SourceComputerId", computerEntity.Id},
                {"SourceComputerCertainty", "High"},
                {"SourceComputerResolutionMethod", new BsonArray(new[] {"RpcNtlm"})},
                {"DestinationIpAddress", "[daf::200]"},
                {"DestinationPort", 445},
                {"DestinationComputerName", destinationComputerName},
                {"DestinationComputerId", domainController.Id},
                {"DestinationComputerCertainty", "High"},
                {"DestinationComputerResolutionMethod", new BsonArray(new[] {"RpcNtlm"})},
                {"DomainControllerStartTime", dateTime},
                {"TransportProtocol", "Tcp"},
                {"NtStatus", "Success"},
                {"OperationType", queryType.ToString()},
                {"DomainId", domainId}
            };
            switch (queryType)
            {
                case SamrViewModel.SamrQueryType.QueryUser:
                    networkActivityDocument.Add("UserName", queriedEntityObject?.Name);
                    networkActivityDocument.Add("UserId", queriedEntityObject?.Id);
                    networkActivityDocument.Add("IsSensitiveUser", BsonBoolean.True);
                    break;
                case SamrViewModel.SamrQueryType.QueryGroup:
                    networkActivityDocument.Add("GroupName", queriedEntityObject?.Name);
                    networkActivityDocument.Add("GroupId", queriedEntityObject?.Id);
                    networkActivityDocument.Add("IsSensitiveGroup", BsonBoolean.True);
                    break;
                case SamrViewModel.SamrQueryType.EnumerateGroups:
                    break;
                case SamrViewModel.SamrQueryType.EnumerateUsers:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(queryType), queryType, null);
            }
            return networkActivityDocument;
        }

        public static byte[] ComputeUnsecureHash(this byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
}