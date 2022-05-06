﻿namespace DevOpsManagement.DevOpsAPI
{
    using DevOpsManagement.Model;
    using Flurl;
    using Flurl.Http;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Linq;
    using System.Net;
    using System.Text.Json;
    using System.Threading.Tasks;

    public static class Project
    {  
        // OAuth Version
        public static async Task<JsonDocument> GetProjectsAsync(Url _organization, string token)
        {
            var queryResponse = await $"{_organization}"
                .AppendPathSegment("_apis/projects")
                .SetQueryParam("api-version", Constants.APIVERSION)
                .WithOAuthBearerToken(token)
                .GetAsync();

            return JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync());
        }

        public static async Task<JsonDocument> GetProjectStatusAsync(Url _organization, string operationId, string accessToken)
        {
            // GET https://dev.azure.com/{organization}/_apis/operations/{operationId}?api-version=6.1-preview.1
            var queryResponse = await $"{_organization}"
                .AppendPathSegment("_apis/operations")
                .AppendPathSegment(operationId)
                .SetQueryParam("api-version", Constants.APIVERSION)
                .WithOAuthBearerToken(accessToken)
                .GetAsync();

            return JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync());
        }

        public static async Task<JsonDocument> GetProjectAsync(Url _organization, string projectName, string acccessToken)
        {
            string responseContent = "";
            // GET https://dev.azure.com/{organization}/_apis/projects/{projectId}/properties?api-version=6.1-preview.1
            var queryResponse = await $"{_organization}"
                .AppendPathSegment($"_apis/projects/{projectName}")
                .SetQueryParam("api-version", Constants.APIVERSION)
                .WithOAuthBearerToken(acccessToken)
                .GetAsync();

            responseContent = await queryResponse.ResponseMessage.Content.ReadAsStringAsync();
            Log.Information("Query result = {@received}", responseContent);
            return JsonDocument.Parse(responseContent);
        }

        public static async Task<JsonDocument> GetProjectByNameAsync(Url _organization, string projectName, string accessToken)
        {
            var queryResponse = await $"{_organization}"
                .AppendPathSegment($"_apis/projects/{projectName}")
                .SetQueryParam("api-version", Constants.APIVERSION)
                .WithOAuthBearerToken(accessToken)
                .GetAsync();

            return JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync());
        }

        public static async Task<JsonDocument> GetIdentityForGroupAsync(string organizationName, string projectName, string groupName, string accessToken)
        {
            //https://vssps.dev.azure.com/{{organization}}/_apis/identities?searchFilter=General&filterValue=[{{project}}]\Contributors&queryMembership=None&api-version={{api-version-preview}}
            var queryResponse = await $"https://vssps.dev.azure.com/{organizationName}"
                .AppendPathSegment($"_apis/identities")
                .SetQueryParam("searchFilter", "General")
                .SetQueryParam("filterValue", $"[{projectName}]\\{groupName}")
                .SetQueryParam("queryMembership", "None")
                .SetQueryParam("api-version", Constants.APIVERSION)
                .WithOAuthBearerToken(accessToken)
                .GetAsync();

            return JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync());
        }

        public static async Task<JsonDocument> GetIdentityForOrganizationAsync(string organizationName, string groupName, string accessToken)
        {
            //https://vssps.dev.azure.com/{{organization}}/_apis/identities?searchFilter=General&filterValue=[{{project}}]\Contributors&queryMembership=None&api-version={{api-version-preview}}
            var queryResponse = await $"https://vssps.dev.azure.com/{organizationName}"
                .AppendPathSegment($"_apis/identities")
                .SetQueryParam("searchFilter", "General")
                .SetQueryParam("filterValue", $"[{organizationName}]\\{groupName}")
                .SetQueryParam("queryMembership", "None")
                .SetQueryParam("api-version", Constants.APIVERSION)
                .WithOAuthBearerToken(accessToken)
                .GetAsync();

            return JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync());
        }

        public static async Task<string> CreateProjectsAsync(Url _organization,
            string projectName,
            string projectDescription,
            string processTemplateId,
            string accessToken)
        {
            string operationId = string.Empty;
            string response = string.Empty;

            var project = new
            {
                name = projectName,
                description = projectDescription,
                visibility= "private",
                capabilities = new
                {
                    versioncontrol = new
                    {
                        sourceControlType = "Git"
                    },
                    processTemplate = new
                    {
                        templateTypeId = processTemplateId
                    }
                }
            };

            var queryResponse = await $"{_organization}"
                .AppendPathSegment("_apis/projects")
                .SetQueryParam("api-version", Constants.APIVERSION)
                .WithOAuthBearerToken(accessToken)
                .AllowHttpStatus(HttpStatusCode.BadRequest)
                .PostJsonAsync(project);

            if(!queryResponse.ResponseMessage.IsSuccessStatusCode)
            {
                Log.Error("*** CreateProjectsAsync failed with {@Response} ***", queryResponse);
                Log.Error("*** Call details: {@Project} ***", project);
                response = await queryResponse.ResponseMessage.Content.ReadAsStringAsync();
                Log.Error("*** Response Body: {0} ***", response);
                throw new ApplicationException("*** Project creation failed ***");
            }

            try
            {
                response = await queryResponse.ResponseMessage.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<CreateProjectResponse>(response);

                operationId= responseObject.Id;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"*** Cannot parse CreateProject response: {response} ***");
                throw;
            }

            return operationId;
        }


        public static async Task<JsonDocument> GetWorkItemByIdAsync(string organizationName, string projectName, int workitemId, string accessToken)
        {
            // GET https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{id}?fields={fields}&asOf={asOf}&$expand={$expand}&api-version=6.1-preview.3
            var queryResponse = await $"https://dev.azure.com/{organizationName}"
               .AppendPathSegment(projectName)
               .AppendPathSegment($"_apis/wit/workitems/{workitemId}")
               .SetQueryParam("api-version", "6.0")
               .WithOAuthBearerToken(accessToken)
               .GetAsync();

            return JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync());
        }

        public static async Task<JsonDocument> UpdateWorkItemByIdAsync(Url organization, int workitemId, object patchOperation, string accessToken)
        {
            // PATCH https://dev.azure.com/fabrikam/_apis/wit/workitems/{id}?api-version=6.1-preview.3
            var queryResponse = await organization
               .AppendPathSegment($"_apis/wit/workitems/{workitemId}")
               .SetQueryParam("api-version", "6.0")
               .WithHeader("Content-Type", "application/json-patch+json")
               .WithOAuthBearerToken(accessToken)
               .PatchJsonAsync(patchOperation);

            return JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync());
        }

        public static async Task<JsonDocument> AddWorkItemCommentAsync(Url organization, string projectId, int workitemId, string comment, string mention, string accessToken)
        {
            // POST https://dev.azure.com/{organization}/{project}/_apis/wit/workItems/{workItemId}/comments?api-version=6.1-preview.3

            var commentPayload = new
            {
                text = $"<div><a href =\"#\"data-vss-mention=\"version:2.0,63fab158-69d5-4bc4-8a5a-1033f1cf3ee5\">@{mention}</a>&nbsp;{comment}</div>"
            };

            var queryResponse = await organization
               .AppendPathSegment(projectId)
               .AppendPathSegment($"_apis/wit/workitems/{workitemId}/comments")
               .SetQueryParam("api-version", "6.1-preview.3")
               .WithOAuthBearerToken(accessToken)
               .PostJsonAsync(commentPayload);

            return JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync());
        }

        public static async Task<int> GetMaxAzIdForEnvironment(string organizationName, string projectName, string projectTeam, string acccessToken)
        {
            var wiql = new
            {
                query = "SELECT [Custom.AZP_ID] \n" +
            "FROM workitems \n" +
            "WHERE [System.TeamProject] = @project \n" +
            "       and [System.WorkItemType]=\"Project\" \n" +
            "ORDER BY [Custom.AZP_ID] DESC"
            };

            // POST https://dev.azure.com/{organization}/{project}/{team}/_apis/wit/wiql?timePrecision={timePrecision}&$top={$top}&api-version=4.1
            var queryResponse = await $"https://dev.azure.com/{organizationName}"
               .AppendPathSegment(projectName)
               .AppendPathSegment(projectTeam)
               .AppendPathSegment($"_apis/wit/wiql")
               .SetQueryParam("$top", "1")
               .SetQueryParam("api-version", "6.0")
               .WithOAuthBearerToken(acccessToken)
               .PostJsonAsync(wiql);

            var azid = 0;
            try
            {
                var jsonQueryResult = JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync());
                var workItemWithmaxAzId = jsonQueryResult.RootElement.GetProperty("workItems").EnumerateArray().First().GetProperty("id").GetInt32();
                var workItem = await GetWorkItemByIdAsync(organizationName, projectName, workItemWithmaxAzId, acccessToken);
                azid = workItem.RootElement.GetProperty("fields").GetProperty("Custom.AZP_ID").GetInt32();
            }
            catch (InvalidOperationException)
            {
                Log.Warning("*** Failed to get AZP_ID from workitem --> starting with 0");
            }

            return azid;
        }

        public static async Task<JsonDocument> GetProjectDescriptorAsync(string organizationName, string projectId, string accessToken)
        {
            // GET https://vssps.dev.azure.com/{{organization}}/_apis/graph/descriptors/95873e02-95f8-40cf-bce6-e563c7cd5fdf?api-version={{api-version-preview}}
            var queryResponse = await $"https://vssps.dev.azure.com/{organizationName}"
                .AppendPathSegment($"_apis/graph/descriptors/{projectId}")
                .SetQueryParam("api-version", Constants.APIVERSION)
                .WithOAuthBearerToken(accessToken)
                .GetAsync();

            return JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// This triggers the creation of the Endpoint Administrators group
        /// </summary>
        /// <param name="_organization"></param>
        /// <param name="projectName"></param>
        /// <param name="pat"></param>
        /// <returns></returns>
        internal static async Task TriggerEndpointAdminGroupCreationAsync(Url _organization, string projectId, string accessToken)
        {
            var dummyServiceConnection = new
            {
                authorization = new
                {
                    scheme = "UsernamePassword",
                    parameters = new { username = "", password = "" }
                },
                name = "donotuse",
                serviceEndpointProjectReferences = new[]
                {
                    new {
                        name="dummyServiceConnection",
                        projectReference = new
                        {
                            id = projectId
                        }
                    }
                },
                type = "generic",
                url = "https://bing.com",
                isShared = false,
                owner = "library"
            };

            var queryResponse = await $"{_organization}"
                .AppendPathSegment("_apis/serviceendpoint/endpoints")
                .SetQueryParam("api-version", "6.0-preview.4")
                .WithOAuthBearerToken(accessToken)
                .PostJsonAsync(dummyServiceConnection);

            if (queryResponse.ResponseMessage.IsSuccessStatusCode)
            {
                var serviceEndpointId = JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString();
                //Service Endpoint created --> EndpointAdministrator group created --> cleanup dummy service endppoint
                var deleteResponse = await $"{_organization}"
                    .AppendPathSegment($"_apis/serviceendpoint/endpoints/{serviceEndpointId}")
                    .SetQueryParam("projectIds", projectId)
                    .SetQueryParam("api-version", "6.1-preview.4")
                    .WithOAuthBearerToken(accessToken)
                    .DeleteAsync();

                return;
            }
            else
                throw new ApplicationException("Could not create service endpoint");
        }

        /// <summary>
        /// This triggers the creation of the Deployment Group Administrators group
        /// </summary>
        /// <param name="_organization"></param>
        /// <param name="projectName"></param>
        /// <param name="pat"></param>
        /// <returns></returns>
        internal static async Task TriggerDeploymentGroupAdminGroupCreationAsync(Url _organization, string projectId, string accessToken)
        {
            var dummyDeploymentGroup = new
            {
                name = "DeleteMe",
                poolId = 0
            };

            var queryResponse = await $"{_organization}"
                .AppendPathSegment(projectId)
                .AppendPathSegment("_apis/distributedtask/deploymentgroups")
                .SetQueryParam("api-version", "6.0-preview.1")
                .WithOAuthBearerToken(accessToken)
                .PostJsonAsync(dummyDeploymentGroup);

            if (queryResponse.ResponseMessage.IsSuccessStatusCode)
            {
                var groupId = JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
                var deleteResponse = await $"{_organization}"
                    .AppendPathSegment(projectId)
                    .AppendPathSegment($"_apis/distributedtask/deploymentgroups/{groupId}")
                    .SetQueryParam("api-version", "6.1-preview.1")
                    .WithOAuthBearerToken(accessToken)
                    .DeleteAsync();

                return;
            }
            else
                throw new ApplicationException("Could not create Deployment Group");
        }

        /// <summary>
        /// This triggers the creation of the Release Administrators group
        /// </summary>
        /// <param name="_organization"></param>
        /// <param name="projectName"></param>
        /// <param name="pat"></param>
        /// <returns></returns>
        internal static async Task TriggerReleaseAdminGroupCreationAsync(string orgaName, string projectName, string accessToken)
        {
            string dummyReleaseDefinition = "{\"id\":0,\"name\":\"DeleteMe\",\"source\":2,\"comment\":\"\",\"createdOn\":\"2021-05-20T09:52:01.369Z\",\"createdBy\":null,\"modifiedBy\":null,\"modifiedOn\":\"2021-05-20T09:52:01.369Z\",\"environments\":[{\"id\":-2,\"name\":\"Stage 1\",\"rank\":1,\"variables\":{},\"variableGroups\":[],\"preDeployApprovals\":{\"approvals\":[{\"rank\":1,\"isAutomated\":true,\"isNotificationOn\":false,\"id\":0}],\"approvalOptions\":{\"executionOrder\":1}},\"deployStep\":{\"tasks\":[],\"id\":0},\"postDeployApprovals\":{\"approvals\":[{\"rank\":1,\"isAutomated\":true,\"isNotificationOn\":false,\"id\":0}],\"approvalOptions\":{\"executionOrder\":2}},\"deployPhases\":[{\"deploymentInput\":{\"parallelExecution\":{\"parallelExecutionType\":0},\"agentSpecification\":{\"metadataDocument\":\"https://mmsprodweu1.vstsmms.visualstudio.com/_apis/mms/images/VS2017/metadata\",\"identifier\":\"vs2017-win2016\",\"url\":\"https://mmsprodweu1.vstsmms.visualstudio.com/_apis/mms/images/VS2017\"},\"skipArtifactsDownload\":false,\"artifactsDownloadInput\":{},\"demands\":[],\"enableAccessToken\":false,\"timeoutInMinutes\":0,\"jobCancelTimeoutInMinutes\":1,\"condition\":\"succeeded()\",\"overrideInputs\":{},\"dependencies\":[]},\"rank\":1,\"phaseType\":1,\"name\":\"Agent job\",\"refName\":null,\"workflowTasks\":[],\"phaseInputs\":{\"phaseinput_artifactdownloadinput\":{\"artifactsDownloadInput\":{},\"skipArtifactsDownload\":false}}}],\"runOptions\":{},\"environmentOptions\":{\"emailNotificationType\":\"OnlyOnFailure\",\"emailRecipients\":\"release.environment.owner;release.creator\",\"skipArtifactsDownload\":false,\"timeoutInMinutes\":0,\"enableAccessToken\":false,\"publishDeploymentStatus\":true,\"badgeEnabled\":false,\"autoLinkWorkItems\":false,\"pullRequestDeploymentEnabled\":false},\"demands\":[],\"conditions\":[{\"conditionType\":1,\"name\":\"ReleaseStarted\",\"value\":\"\"}],\"executionPolicy\":{\"concurrencyCount\":1,\"queueDepthCount\":0},\"schedules\":[],\"properties\":{\"LinkBoardsWorkItems\":false,\"BoardsEnvironmentType\":\"unmapped\"},\"preDeploymentGates\":{\"id\":0,\"gatesOptions\":null,\"gates\":[]},\"postDeploymentGates\":{\"id\":0,\"gatesOptions\":null,\"gates\":[]},\"environmentTriggers\":[],\"owner\":{\"displayName\":\"Rainer Nasch 🍬\",\"id\":\"63fab158-69d5-4bc4-8a5a-1033f1cf3ee5\",\"isAadIdentity\":true,\"isContainer\":false,\"uniqueName\":\"rainern@microsoft.com\",\"url\":\"https://dev.azure.com/pocit/\"},\"retentionPolicy\":{\"daysToKeep\":30,\"releasesToKeep\":3,\"retainBuild\":true},\"processParameters\":{}}],\"artifacts\":[],\"variables\":{},\"variableGroups\":[],\"triggers\":[],\"lastRelease\":null,\"tags\":[],\"path\":\"\\\\\",\"properties\":{\"DefinitionCreationSource\":\"ReleaseNew\",\"IntegrateJiraWorkItems\":\"false\",\"IntegrateBoardsWorkItems\":false},\"releaseNameFormat\":\"Release-$(rev:r)\",\"description\":\"\"}";

            var queryResponse = await $"https://vsrm.dev.azure.com/{orgaName}"
                .AppendPathSegment(projectName)
                .AppendPathSegment("_apis/Release/definitions")
                .WithHeader("Content-Type", "application/json")
                .SetQueryParam("api-version", "6.0-preview.4")
                .WithOAuthBearerToken(accessToken)
                .PostStringAsync(dummyReleaseDefinition);

            if (queryResponse.ResponseMessage.IsSuccessStatusCode)
            {
                var definitionId = JsonDocument.Parse(await queryResponse.ResponseMessage.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
                var deleteResponse = await $"https://vsrm.dev.azure.com/{orgaName}"
                    .AppendPathSegment(projectName)
                    .AppendPathSegment($"_apis/Release/definitions/{definitionId}")
                    .WithHeader("Content-Type", "application/json")
                    .SetQueryParam("api-version", "6.1-preview.4")
                    .WithOAuthBearerToken(accessToken)
                    .DeleteAsync();
                return;
            }
            else
                throw new ApplicationException("Could not create a Release Definition pipeline");
        }

    }
}
