using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json;

namespace interflow
{
    class interflow
    {
        public static Dictionary<string, dynamic> getValidation(String apiKey)
        {
            /*
            input:
			    apiKey - api key for interflow

		    description:
			    Checks if the given API key is valid, and if it is, returns information about the owner

		    output:
			    returns a dictionary:
			    {
				    "response": <int>, 		#http response code
				    "data": <dictionary> 	#contents of the remote folder
			    }
			
			    dictionary deffinition for data
			    "data": {
						    "Id": <string>, 				#Id of user
						    "FirstName": <string>, 			#first name of user
						    "LastName": <string>, 			#last name of user
						    "email": <string>, 				#email of user
						    "RegistrationDate": <string>, 	#registration date of user",
						    "AuthenticationType": <string>, #auth type of user
						    "Association": <string>, 		#organization of user
						    "IsAuthenticated": <bool>, 		#if user is authenticated
						    "Name": <string> 				#full name of user
					    }
					
	        */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            var uri = "https://interflow.azure-api.net/api/debug/who?" + queryString;

            var response = client.GetAsync(uri).Result;

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                dynamic ReturnData = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                Results.Add("data", ReturnData["Identity"]);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public static Dictionary<string, dynamic> getInterflowDirs(String apiKey, String path)
        {
            /*
            input:
                apiKey - api key for interflow
                path   - directory in interflow to to search for directories 

            description:
                Gets the list of directory names in a given directory

            output:
                returns a dictionary:
                {
                    "response": <int>, 		#http response code
                    "data": array[<string>]	#contents of the remote folder
                }
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            queryString["directoryPath"] = path;
            var uri = "https://interflow.azure-api.net/api/downloads/directorylist?" + queryString;

            var response = client.GetAsync(uri).Result;

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                dynamic ReturnData = JsonConvert.DeserializeObject<List<string>>(response.Content.ReadAsStringAsync().Result);

                List<string> SlashAppendedData = new List<string>();
                foreach(var Dir in ReturnData)
                {
                    SlashAppendedData.Add(Dir + "/");
                }
                Results.Add("data", SlashAppendedData);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public static Dictionary<string, dynamic> getInterflowFiles(String apiKey, String path)
        {
            /*
            input:
                apiKey - api key for interflow
                path   - directory in interflow to to search for files 

            description:
                Gets the list of files in a given directory

            output:
                returns a dictionary:
                {
                    "response": <int>, 			#http response code
                    "data": array[<dictionary>]	#contents of the remote folder
                }

                dictionary deffinition for data
                "data": [
                        {
                            "Name": <string>, 			#file name
                            "Length": <int>, 			#length in bytes
                            "RelativePath": <string>, 	#full path to file
                            "LastModified": <string> 	#last file modification
                        }
                        ]
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            queryString["directoryPath"] = path;
            var uri = "https://interflow.azure-api.net/api/downloads/filelist?" + queryString;

            var response = client.GetAsync(uri).Result;

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                var apiresponse = response.Content.ReadAsStringAsync().Result;
                dynamic ReturnData = JsonConvert.DeserializeObject<List<Dictionary<string,string>>>(apiresponse);
                Results.Add("data", ReturnData);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public static Dictionary<string, dynamic> getInterflowFileNamesOnly(String apiKey, String path)
        {
            /*
            input:
			    apiKey - api key for interflow
			    path   - directory in interflow to to search for files 

		    description:
			    Gets the list of file names in a given directory

		    output:
			    returns a dictionary:
			    {
				    "response": <int>, 		#http response code
				    "data": array[<string>]	#contents of the remote folder, names only
			    }
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            queryString["directoryPath"] = path;
            var uri = "https://interflow.azure-api.net/api/downloads/filelist?" + queryString;

            var response = client.GetAsync(uri).Result;

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                var ReturnData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(response.Content.ReadAsStringAsync().Result);
                List<String> filenames = new List<string>();
                foreach (var file in ReturnData)
                {
                    //Console.Write(file);
                    filenames.Add(file["Name"]);
                }
                Results.Add("data", filenames);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public static Dictionary<string, dynamic> downloadInterflowFile(String apiKey, String path, String file)
        {
            /*
             intput:
         		apiKey - api key for interflow
			    path   - directory in interflow to download newest file from
			    file   - name of the file to fetch

		    description:
			    Gets the contents of the specified file in an interflow directory

		    output:
			    returns a dictionary:
			    {
				    "response": <int>, 	#http response code
				    "data": <byte[]> 	#contents of the remote file
			    }
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            client.Timeout = TimeSpan.FromMinutes(30);
            queryString["directoryPath"] = path;
            var uri = "https://interflow.azure-api.net/api/downloads/file?fileName=" + file + "&" + queryString;

            var response = client.GetAsync(uri).Result;

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {

                Results.Add("data", response.Content.ReadAsByteArrayAsync().Result);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public static int UploadInterflowGenericFile(String apiKey, String filepath)
        {
            /*
            input:

                apiKey - api key for interflow
                filepath - The path to the file you wish to upload

            description:
                Uploads a generic file to interflow for analysis.


            output:
                returns an int: response code from the API call.
                202 File upload sucessful.
                204 No Content: The request does not contain any file. //this should never be a return code the user encounters.
                409 Conflict: File already exists.
                415 Unsupported media type.
                500 Internal server error"
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            String FileName = System.IO.Path.GetFileName(filepath);


            MultipartFormDataContent form = new MultipartFormDataContent();
            byte[] FileContent;
            try
            {
                FileContent = System.IO.File.ReadAllBytes(filepath);
            }
            catch (Exception e)
            {
                Console.Error.Write("*** ERROR upload file not found - " + e.Message);
                return -1;
            }
            form.Add(new ByteArrayContent(FileContent, 0, FileContent.Count()), FileName, FileName);

            HttpResponseMessage response = client.PostAsync("https://interflow.azure-api.net/api/file/upload", form).Result;

            client.Dispose();
            return (int)response.StatusCode;

        }


        public static int UploadInterflowOneIndicator(String apiKey, String OneIndicatorJson)
        {
            /*
            input:

                apiKey - api key for interflow
                OneIndicatorJson - The array of json one indicators
                
            description:
                Uploads a OneIndicator via a json string.


            output:
                returns an int: response code from the API call.
                201 Indicator upload sucessful.
                400 One or more indicators is not valid.
                409 One or more indicators was not received or accepted by the hub.
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            client.DefaultRequestHeaders.Add("Accept-Encoding", "UTF-8");

            HttpResponseMessage response = client.PostAsync("https://interflow.azure-api.net/api/indicators/upload", new StringContent(OneIndicatorJson, Encoding.UTF8, "application/json")).Result;

            client.Dispose();
            return (int)response.StatusCode;

        }

        public static Dictionary<string, dynamic> getInterflowSharedFiles(String apiKey)
        {
            /*
            input:
                apiKey - api key for interflow

            description:
                Gets the list of shared files

            output:
                returns a dictionary:
                {
                    "response": <int>, 			#http response code
                    "data": array[<dictionary>]	#shared file data
                }

                dictionary deffinition for data
                "data": [
                        {
                            "Name": <string>, 			#file name
                            "Length": <int>, 			#length in bytes
                            "RelativePath": <string>, 	#full path to file
                            "LastModified": <string> 	#last file modification
                        }
                        ]
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            var uri = "https://interflow.azure-api.net/api/file/listsharedfiles";

            var response = client.GetAsync(uri).Result;

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                var apiresponse = response.Content.ReadAsStringAsync().Result;
                dynamic ReturnData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(apiresponse);
                Results.Add("data", ReturnData);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public static Dictionary<string, dynamic> getInterflowSharedFileNamesOnly(String apiKey)
        {
            /*
            input:
                apiKey - api key for interflow

            description:
                Gets the list of shared files

            output:
                returns a dictionary:
                {
                    "response": <int>, 			#http response code
                    "data": array[<string>]	#file names of shared files
                }

            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            var uri = "https://interflow.azure-api.net/api/file/listsharedfiles";

            var response = client.GetAsync(uri).Result;

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                var ReturnData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(response.Content.ReadAsStringAsync().Result);
                List<String> filenames = new List<string>();
                foreach (var file in ReturnData)
                {
                    //Console.Write(file);
                    filenames.Add(file["Name"]);
                }
                Results.Add("data", filenames);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public static Dictionary<string, dynamic> downloadInterflowSharedFile(String apiKey, String file)
        {
            /*
             intput:
         		apiKey - api key for interflow
			    file   - name of the file to fetch

		    description:
			    Gets the contents of the specified shared file

		    output:
			    returns a dictionary:
			    {
				    "response": <int>, 	#http response code
				    "data": <byte[]> 	#contents of the remote file
			    }
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            client.Timeout = TimeSpan.FromMinutes(30);
            var uri = "https://interflow.azure-api.net/api/file/download?fileName=" + file;

            var response = client.GetAsync(uri).Result;

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {

                Results.Add("data", response.Content.ReadAsByteArrayAsync().Result);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        //-------- async version of functions below --------//
        public async static Task<Dictionary<string, dynamic>> getValidation_async(String apiKey)
        {
            /*
            input:
			    apiKey - api key for interflow

		    description:
			    Checks if the given API key is valid, and if it is, returns information about the owner

		    output:
			    returns a Task<dictionary<string, dynamic>> where the inner dictionary contains:
			    {
				    "response": <int>, 		#http response code
				    "data": <dictionary> 	#contents of the remote folder
			    }
			
			    dictionary deffinition for data
			    "data": {
						    "Id": <string>, 				#Id of user
						    "FirstName": <string>, 			#first name of user
						    "LastName": <string>, 			#last name of user
						    "email": <string>, 				#email of user
						    "RegistrationDate": <string>, 	#registration date of user",
						    "AuthenticationType": <string>, #auth type of user
						    "Association": <string>, 		#organization of user
						    "IsAuthenticated": <bool>, 		#if user is authenticated
						    "Name": <string> 				#full name of user
					    }
					
	        */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            var uri = "https://interflow.azure-api.net/api/debug/who?" + queryString;

            var response = await client.GetAsync(uri);

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                var apiresponse = await response.Content.ReadAsStringAsync();
                dynamic ReturnData = JsonConvert.DeserializeObject(apiresponse);
                Results.Add("data", ReturnData["Identity"]);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public async static Task<Dictionary<string, dynamic>> getInterflowDirs_async(String apiKey, String path)
        {
            /*
            input:
                apiKey - api key for interflow
                path   - directory in interflow to to search for directories 

            description:
                Gets the list of directory names in a given directory

            output:
                returns a Task<dictionary<string, dynamic>> where the inner dictionary contains:
                {
                    "response": <int>, 		#http response code
                    "data": array[<string>]	#contents of the remote folder
                }
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            queryString["directoryPath"] = path;
            var uri = "https://interflow.azure-api.net/api/downloads/directorylist?" + queryString;

            var response = await client.GetAsync(uri);

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                var apiresponse = await response.Content.ReadAsStringAsync();
                dynamic ReturnData = JsonConvert.DeserializeObject<List<string>>(apiresponse);

                List<string> SlashAppendedData = new List<string>();
                foreach (var Dir in ReturnData)
                {
                    SlashAppendedData.Add(Dir + "/");
                }
                Results.Add("data", SlashAppendedData);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public async static Task<Dictionary<string, dynamic>> getInterflowFiles_async(String apiKey, String path)
        {
            /*
            input:
                apiKey - api key for interflow
                path   - directory in interflow to to search for files 

            description:
                Gets the list of files in a given directory

            output:
                returns a Task<dictionary<string, dynamic>> where the inner dictionary contains:
                {
                    "response": <int>, 			#http response code
                    "data": array[<dictionary>]	#contents of the remote folder
                }

                dictionary deffinition for data
                "data": [
                        {
                            "Name": <string>, 			#file name
                            "Length": <int>, 			#length in bytes
                            "RelativePath": <string>, 	#full path to file
                            "LastModified": <string> 	#last file modification
                        }
                        ]
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            queryString["directoryPath"] = path;
            var uri = "https://interflow.azure-api.net/api/downloads/filelist?" + queryString;

            var response = await client.GetAsync(uri);

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                var apiresponse = await response.Content.ReadAsStringAsync();
                dynamic ReturnData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(apiresponse);
                Results.Add("data", ReturnData);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public async static Task<Dictionary<string, dynamic>> getInterflowFileNamesOnly_async(String apiKey, String path)
        {
            /*
            input:
			    apiKey - api key for interflow
			    path   - directory in interflow to to search for files 

		    description:
			    Gets the list of file names in a given directory

		    output:
			    returns a Task<Dictionary<string, dynamic>> where the inner dictionary contains the following:
			    {
				    "response": <int>, 		#http response code
				    "data": array[<string>]	#contents of the remote folder, names only
			    }
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            queryString["directoryPath"] = path;
            var uri = "https://interflow.azure-api.net/api/downloads/filelist?" + queryString;

            var response = await client.GetAsync(uri);

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                var Response = await response.Content.ReadAsStringAsync();
                var ReturnData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(Response);
                List<String> filenames = new List<string>();
                foreach (var file in ReturnData)
                {
                    //Console.Write(file);
                    filenames.Add(file["Name"]);
                }
                Results.Add("data", filenames);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public static async Task downloadInterflowFile_async(String apiKey, String path, String file, String outFile)
        {
            /*
                 intput:
                         apiKey - api key for interflow
                         path   - directory in interflow to download newest file from
                         file   - name of the file to fetch
                         outFile- the output directory to write the file to.

                  description:
                         Gets the contents of the specified file in an interflow directory

                  output:
                         Task (async)
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                client.Timeout = TimeSpan.FromMinutes(30);

                var queryString = HttpUtility.ParseQueryString(string.Empty);
                queryString["directoryPath"] = path;
                var uri = "https://interflow.azure-api.net/api/downloads/file?fileName=" + file + "&" + queryString;

                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    using
                        (
                        System.IO.Stream contentStream = await (await client.SendAsync(request)).Content.ReadAsStreamAsync(),
                        stream = new System.IO.FileStream(outFile, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 4096, true)
                        )
                    {
                        await contentStream.CopyToAsync(stream);
                    }
                }
                client.Dispose();
            }
        }

        public async static Task<int> UploadInterflowGenericFile_async (String apiKey, String filepath)
        {
            /*
            input:

                apiKey - api key for interflow
                filepath - The path to the file you wish to upload

            description:
                Uploads a generic file to interflow for analysis.


            output:
                returns a Task<int>: response code from the API call.
                202 File upload sucessful.
                204 No Content: The request does not contain any file. //this should never be a return code the user encounters.
                409 Conflict: File already exists.
                415 Unsupported media type.
                500 Internal server error"
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            String FileName = System.IO.Path.GetFileName(filepath);


            MultipartFormDataContent form = new MultipartFormDataContent();
            byte[] FileContent;
            try
            {
                FileContent = System.IO.File.ReadAllBytes(filepath);
            }
            catch (Exception e)
            {
                Console.Error.Write("*** ERROR upload file not found - " + e.Message);
                return -1;
            }
            form.Add(new ByteArrayContent(FileContent, 0, FileContent.Count()), FileName, FileName);

            HttpResponseMessage response = await client.PostAsync("https://interflow.azure-api.net/api/file/upload", form);

            client.Dispose();
            return (int)response.StatusCode;

        }

        public async static Task<Dictionary<string, dynamic>> getInterflowSharedFiles_async(String apiKey)
        {
            /*
            input:
                apiKey - api key for interflow

            description:
                Gets the list of shared files

            output:
                returns a Task<dictionary<string, dynamic>> where the inner dictionary contains:
                {
                    "response": <int>, 			#http response code
                    "data": array[<dictionary>]	#shared file data
                }

                dictionary deffinition for data
                "data": [
                        {
                            "Name": <string>, 			#file name
                            "Length": <int>, 			#length in bytes
                            "RelativePath": <string>, 	#full path to file
                            "LastModified": <string> 	#last file modification
                        }
                        ]
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            var uri = "https://interflow.azure-api.net/api/file/listsharedfiles";

            var response = await client.GetAsync(uri);

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                var apiresponse = await response.Content.ReadAsStringAsync();
                dynamic ReturnData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(apiresponse);
                Results.Add("data", ReturnData);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public async static Task<Dictionary<string, dynamic>> getInterflowSharedFileNamesOnly_async(String apiKey)
        {
            /*
            input:
                apiKey - api key for interflow

            description:
                Gets the list of shared files

            output:
                returns a Task<dictionary<string, dynamic>> where the inner dictionary contains:
                {
                    "response": <int>, 			#http response code
                    "data": array[<string>]	#file names of shared files
                }
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            var uri = "https://interflow.azure-api.net/api/file/listsharedfiles";

            var response = await client.GetAsync(uri);

            Dictionary<string, dynamic> Results = new Dictionary<string, dynamic>();

            Results.Add("response", (int)response.StatusCode);
            if ((int)response.StatusCode == 200)
            {
                var Response = await response.Content.ReadAsStringAsync();
                var ReturnData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(Response);
                List<String> filenames = new List<string>();
                foreach (var file in ReturnData)
                {
                    //Console.Write(file);
                    filenames.Add(file["Name"]);
                }
                Results.Add("data", filenames);
            }
            else
            {
                Results.Add("data", null);
            }
            client.Dispose();
            return Results;
        }

        public static async Task downloadInterflowSharedFile_async(String apiKey, String file, String outFile)
        {
            /*
                 intput:
                         apiKey - api key for interflow
                         file   - name of the file to fetch
                         outFile- the output directory to write the file to.

                  description:
                         Gets the contents of the specified shared file

                  output:
                         Task (async)
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                client.Timeout = TimeSpan.FromMinutes(30);

                var queryString = HttpUtility.ParseQueryString(string.Empty);
                var uri = "https://interflow.azure-api.net/api/file/download?fileName=" + file;

                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    using
                        (
                        System.IO.Stream contentStream = await (await client.SendAsync(request)).Content.ReadAsStreamAsync(),
                        stream = new System.IO.FileStream(outFile, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 4096, true)
                        )
                    {
                        await contentStream.CopyToAsync(stream);
                    }
                }
                client.Dispose();
            }
        }

        public static async Task<int> UploadInterflowOneIndicator_async(String apiKey, String OneIndicatorJson)
        {
            /*
            input:

                apiKey - api key for interflow
                OneIndicatorJson - The array of json one indicators

            description:
                Uploads a OneIndicator via a json string.


            output:
                returns an int: response code from the API call.
                201 Indicator upload sucessful.
                400 One or more indicators is not valid.
                409 One or more indicators was not received or accepted by the hub.
            */

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            client.DefaultRequestHeaders.Add("Accept-Encoding", "UTF-8");

            HttpResponseMessage response = await client.PostAsync("https://interflow.azure-api.net/api/indicators/upload", new StringContent(OneIndicatorJson, Encoding.UTF8, "application/json"));

            client.Dispose();
            return (int)response.StatusCode;

        }
    }
}
