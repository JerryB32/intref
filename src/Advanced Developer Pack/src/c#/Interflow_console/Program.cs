using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

//Requires commandline 2.0+
//using CommandLine v2.1.1-beta
using CommandLine;
using CommandLine.Text;

//interflow methods
using interflow;
using System.IO;

namespace Interflow_console
{

    //set up verb parser for download
    [Verb("download", HelpText = "Download file(s) from interflow")]
    class DownloadOptions
    {
        [Option('a', "apikey", Required = true, HelpText = "Interflow API key (hex string)")]
        public string apikey { get; set; }

        [Option('o', "output", Required = true, HelpText = "Local folder to output file(s) to.")]
        public string OutputFolder { get; set; }

        //start mutually exclusive argument set
        [Option('f', "file", HelpText = "File(s) to download as a space deliminated list.", Separator = ' ')]
        public IEnumerable<string> DownloadFiles { get; set; }

        [Option('d', "dir", HelpText = "Downloads all files in <dir> to <output>")]
        public string DownloadDirectory { get; set; }

        //end mutually exclusive argument set

    }

    //set up verb parser for list
    [Verb("list", HelpText = "List files and / or directories")]
    class ListOptions
    {
        [Option('a', "apikey", Required = true, HelpText = "Interflow API key (hex string)")]
        public string apikey { get; set; }

        [Option('d', "dir", Required = true, HelpText = "Remote directory to list")]
        public string Directory { get; set; }

        [Option('t', "type", HelpText = "Filter file types by [f]iles or [d]irectories")]
        public string TypeFilter { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Output only file names. If set, list will output all file details in a json output")]
        public bool Verbose { get; set; }

    }

    //set up verb parser for upload
    [Verb("upload", HelpText = "Upload files or OneIndicator deffinitions to interflow")]
    class UploadOptions
    {
        [Option('a', "apikey", HelpText = "Interflow API key (hex string)", Required = true)]
        public string apikey { get; set; }

        [Option('f', "file", HelpText = "Generic file upload")]
        public string UploadFile { get; set; }

        [Option('j', "json", HelpText = "Upload a OneIndicator deffinition via a json string")]
        public string UploadJson { get; set; }

        [Option('i', "indicatorfile", HelpText = "Upload a OneIndicator via file upload")]
        public string UploadIndicatorFile { get; set; }

    }

    class Program
    {
        static int Main(string[] args)
        {

            //parse the args and call the appropriate function to handle the class object returned.
            return CommandLine.Parser.Default.ParseArguments<ListOptions, DownloadOptions, UploadOptions>(args)
                .MapResult(
                 (ListOptions opts) => ParseListOptions(opts),
                 (DownloadOptions opts) => ParseDownloadOptions(opts),
                 (UploadOptions opts) => ParseUploadOptions(opts),
                 errs => 1);
            return 0;
        }


        /// /////////////////////////////////////////////////
        //helper methods
        private static List<String> listDirectories(String apikey, String path)
        {
            //most people will be inclined to use / or "/" for root... where you would need to use ""
            if(path == "/" || path == "\"/\"" || path =="\\" || path == "\"\\\"")
            {
                path = "";
            }

            //and if the path was more than just a slash, then strip any left slashes as well!
            path = path.TrimStart('/');
            path = path.TrimStart('\\');

            Dictionary<string, dynamic> result = interflow.interflow.getInterflowDirs(apikey, path);
            if(result["response"] != 200)
            {
                Console.Error.WriteLine("*** ERROR could not get directory contents of " + path);
                return null;
            }
            else
            {
                return result["data"];
            }
        }

        private static List<String> listFiles(String apikey, String path, bool verbose)
        {
            //most people will be inclined to use / or "/" for root... where you would need to use ""
            if (path == "/" || path == "\"/\"" || path == "\\" || path == "\"\\\"")
            {
                path = "";
            }

            //and if the path was more than just a slash, then strip any left slashes as well!
            path = path.TrimStart('/');
            path = path.TrimStart('\\');

            if (verbose)
            {
                //If we are fetching the root, we can print shared files instead as there will never be normal files there.
                if (path == "")
                {
                    Dictionary<string, dynamic> result = interflow.interflow.getInterflowSharedFiles(apikey);
                    if (result["response"] != 200)
                    {
                        Console.Error.WriteLine("*** ERROR could not get shared interflow files");
                        return null;
                    }
                    else
                    {
                        List<string> ReturnString = new List<string>();
                        foreach (var file in result["data"])
                        {
                            ReturnString.Add(JsonConvert.SerializeObject(file));
                        }
                        return ReturnString;
                    }
                }
                //else get the normal files in the folder.
                else
                {
                    Dictionary<string, dynamic> result = interflow.interflow.getInterflowFiles(apikey, path);
                    if (result["response"] != 200)
                    {
                        Console.Error.WriteLine("*** ERROR could not get file contents of " + path);
                        return null;
                    }
                    else
                    {
                        List<string> ReturnString = new List<string>();
                        foreach (var file in result["data"])
                        {
                            ReturnString.Add(JsonConvert.SerializeObject(file));
                        }
                        return ReturnString;
                    }
                }
            }
            else
            {
                //If we are fetching the root, we can print shared files instead as there will never be normal files there.
                if (path == "")
                {
                    Dictionary<string, dynamic> result = interflow.interflow.getInterflowSharedFileNamesOnly(apikey);
                    if (result["response"] != 200)
                    {
                        Console.Error.WriteLine("*** ERROR could not get shared interflow files");
                        return null;
                    }
                    else
                    {
                        return result["data"];
                    }
                }
                //else get the normal files in the folder.
                else
                {
                    Dictionary<string, dynamic> result = interflow.interflow.getInterflowFileNamesOnly(apikey, path);
                    if (result["response"] != 200)
                    {
                        Console.Error.WriteLine("*** ERROR could not get file contents of " + path);
                        return null;
                    }
                    else
                    {
                        return result["data"];
                    }
                }
            }
        }

        //check to make sure no conflicting upload options are set
        public static bool checkMutualExclusionUpload(UploadOptions UploadClassObj)
        {
            //commandline didn't have a good way to define a **required** set of argumants that were mutually exclusive. This function is called
            //when parsing to validate that commands were set correctly

            int sum = 0;

            if (!String.IsNullOrEmpty(UploadClassObj.UploadFile)) sum++;
            if (!String.IsNullOrEmpty(UploadClassObj.UploadIndicatorFile)) sum++;
            if (!String.IsNullOrEmpty(UploadClassObj.UploadJson)) sum++;

            if (sum == 0)
            {
                Console.Error.WriteLine("*** ERROR verb command 'upload' requires one of the parametors [file, json, indicatorfile]");
                System.Environment.Exit(-1);
            }
            if (sum > 1)
            {
                Console.Error.WriteLine("*** ERROR Too many parametors passed to verb command 'upload'. Only one parametor in [file, json, indicatorfile] can be given.");
                System.Environment.Exit(-1);
            }
            return true;
        }

        //check to make sure no conflicting download options are set
        public static bool checkMutualExclusionDownload(DownloadOptions DownloadClassObj)
        {
            //commandline didn't have a good way to define a **required** set of argumants that were mutually exclusive. This function is called
            //when parsing to validate that commands were set correctly

            int sum = 0;

            if (DownloadClassObj.DownloadFiles.Count() != 0) sum++;
            if (!String.IsNullOrEmpty(DownloadClassObj.DownloadDirectory)) sum++;

            if (sum == 0)
            {
                Console.Error.WriteLine("*** ERROR verb command 'download' requires one of the parametors [file, dir]");
                System.Environment.Exit(-1);
            }
            if (sum > 1)
            {
                Console.Error.WriteLine("*** ERROR Too many parametors passed to verb command 'download'. Only one parametor in [file, dir] can be given.");
                System.Environment.Exit(-1);
            }
            return true;
        }

        //checks to see if the type filter contains illegal characters
        public static bool checkTypeValueString(ListOptions ListClassObj)
        {
            if (! String.IsNullOrEmpty(ListClassObj.TypeFilter))
            {
                if (ListClassObj.TypeFilter != "d" && ListClassObj.TypeFilter != "f")
                {
                    Console.Error.WriteLine("*** ERROR list type filter must be either 'f' or 'd'. user supplied " + ListClassObj.TypeFilter);
                    System.Environment.Exit(-1);
                }
            }
            return true;
        }
        //end helper methods
        /// /////////////////////////////////////////////////

        //method for parsing list verb
        static int ParseListOptions(ListOptions opts)
        {
            //Check to see if file type flags are set incorrectly
            checkTypeValueString(opts);

            //if else tree for handling list options:
            if (String.IsNullOrEmpty(opts.TypeFilter)) //list both files and directories
            {
                Console.Write(String.Join("\n", listDirectories(opts.apikey, opts.Directory)));
                Console.Write("\n");
                Console.Write(String.Join("\n", listFiles(opts.apikey, opts.Directory, opts.Verbose)));
            }
            else if (opts.TypeFilter == "d")//only get dirs
            {
                Console.Write( String.Join("\n", listDirectories(opts.apikey, opts.Directory)));
            }
            else if (opts.TypeFilter == "f")//only get files
            {
                Console.Write( String.Join("\n", listFiles(opts.apikey, opts.Directory, opts.Verbose)));
            }
            return 0;
        }





        static int ParseDownloadOptions(DownloadOptions opts)
        {
            checkMutualExclusionDownload(opts);

            //If else tree for parsing mutually exclusive options
            //file(s) in DownloadFiles list
            if (opts.DownloadFiles.Count() != 0) 
            {
                //for each file in the files list
                foreach (var file in opts.DownloadFiles)
                {
                    //people might be likely to put a proceeding slash...
                    var file_normalized = file.TrimStart('/');

                    String FilePath = Path.GetDirectoryName(file_normalized);
                    String FileName = Path.GetFileName(file_normalized);

                    //file path is blank, its gonna be a shared file!
                    if (FilePath == "")
                    {
                        Dictionary<string, dynamic> Results = interflow.interflow.downloadInterflowSharedFile(opts.apikey, FileName);
                        if (Results["response"] == 200)
                        {
                            try
                            {
                                System.IO.File.WriteAllBytes(Path.Combine(opts.OutputFolder, FileName), Results["data"]);
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine("*** ERROR output directory not found - " + e.Message);
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("*** ERROR downloading file " + file_normalized + " - returned http response: " + Results["response"]);
                        }
                    }
                    else
                    {
                        Dictionary<string, dynamic> Results = interflow.interflow.downloadInterflowFile(opts.apikey, FilePath, FileName);
                        if (Results["response"] == 200)
                        {
                            try
                            {
                                System.IO.File.WriteAllBytes(Path.Combine(opts.OutputFolder, FileName), Results["data"]);
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine("*** ERROR output directory not found - " + e.Message);
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("*** ERROR downloading file " + file_normalized + " - returned http response: " + Results["response"]);
                        }
                    }
                }
            }

            //Downloading a directory (mirror)
            else if (!String.IsNullOrEmpty(opts.DownloadDirectory))
            {
                //first get the list of files in the directory supplied
                List<string> FileList = listFiles(opts.apikey, opts.DownloadDirectory, false);

                foreach(String file in FileList)
                {
                    String FilePath = opts.DownloadDirectory;
                    String FileName = Path.GetFileName(file);
                    Dictionary<string, dynamic> Results;
                    if (FilePath == "" || FilePath == "/" || FilePath == "\"/\"" || FilePath == "\\" || FilePath == "\"\\\"")
                    {
                        Results = interflow.interflow.downloadInterflowSharedFile(opts.apikey, FileName);
                    }
                    else
                    {
                        //similar to how we trim in the listFiles function, we also trim here to prevent any / in the beginning from being sent to the api endpoint.
                        FilePath = FilePath.TrimStart('/');
                        FilePath = FilePath.TrimStart('\\');
                        Results = interflow.interflow.downloadInterflowFile(opts.apikey, FilePath, FileName);
                    }
                    if (Results["response"] == 200)
                    {
                        try
                        {
                            System.IO.File.WriteAllBytes(Path.Combine(opts.OutputFolder, FileName), Results["data"]);
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine("*** ERROR output directory not found - " + e.Message);
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("*** ERROR downloading file " + file + " - returned http response: " + Results["response"]);
                    }
                }
            }

            return 0;
        }



        static int ParseUploadOptions(UploadOptions opts)
        {
            checkMutualExclusionUpload(opts);

            //If else tree for flag processing
            //if uploading a generic file
            if (!String.IsNullOrEmpty(opts.UploadFile))
            {

                try
                {
                    int responseCode = interflow.interflow.UploadInterflowGenericFile(opts.apikey, opts.UploadFile);
                    if (responseCode == 202) Console.WriteLine("File upload sucessful.");
                    else if (responseCode == 204) Console.Error.WriteLine("*** ERROR No Content: The request does not contain any file."); //this should never be a return code the user encounters.
                    else if (responseCode == 409) Console.Error.WriteLine("*** ERROR Conflict: File already exists.");
                    else if (responseCode == 415) Console.Error.WriteLine("*** ERROR Unsupported media type.");
                    else if (responseCode == 500) Console.Error.WriteLine("*** ERROR Internal server error");
                    else Console.Error.WriteLine("*** ERROR File upload failed with status code " + responseCode);
                }
                catch
                {
                    Console.Error.WriteLine("*** ERROR could not upload file. Please check the file path!");
                }
            }
            //if uploading a json oneIdicator file
            if (!String.IsNullOrEmpty(opts.UploadIndicatorFile))
            {
                try
                {
                    int responseCode = interflow.interflow.UploadInterflowOneIndicator(opts.apikey, File.ReadAllText(opts.UploadIndicatorFile));
                    if (responseCode == 201) Console.WriteLine("File upload sucessful.");
                    else if (responseCode == 400) Console.Error.WriteLine("*** ERROR No data.");
                    else if (responseCode == 500) Console.Error.WriteLine("*** ERROR Internal server error");
                    else Console.Error.WriteLine("*** ERROR File upload failed with status code " + responseCode);
                }
                catch
                {
                    Console.Error.WriteLine("*** ERROR could not upload file. Please check the file path!");
                }
            }
            //if uploading a json string oneIndicator deffinition
            if (!String.IsNullOrEmpty(opts.UploadJson))
            {

                int responseCode = interflow.interflow.UploadInterflowOneIndicator(opts.apikey, opts.UploadJson.Replace("\\", ""));

                if (responseCode == 201) Console.WriteLine("Indicator upload sucessful.");
                else if (responseCode == 400) Console.Error.WriteLine("One or more indicators is not valid.");
                else if (responseCode == 409) Console.Error.WriteLine("One or more indicators was not received or accepted by the hub.");
                else Console.Error.WriteLine("*** ERROR Indicator upload failed with status code " + responseCode);
            }

            return 0;
        }
    }
}
