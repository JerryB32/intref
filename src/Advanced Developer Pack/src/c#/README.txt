Dependencies:

	GUI_interflow:
		Newtonsoft.JSON.              can be installed though nuget.
	
	Interflow_console:
		Newtonsoft.JSON.              can be installed though nuget.
		CommandLineParser.2.1.1-beta. can be installed though nuget. use the -Pre flag to install the beta version.
			(https://github.com/gsscoder/commandline)
		
		
	Once these packages have been installed, the projects should compile.

	For help on installing nuget packages, please refer to this guide: https://docs.microsoft.com/en-us/nuget/tools/package-manager-ui
	
	
-------------------------------------------------
	
	
Using:

	GUI_interflow:
		Please see the `Getting Started.docx` document in the root folder of this distributable.
	
	Interflow_console:
		This application uses verb commands, similar to git. Each verb acts like its own program with its own set of flags which can be used.
		All flags have a short hand and long hand reference; all short hand flags are denoted with a signle dash and a letter (-h) where long hand commands use two dashes (--help)
		
		This usage help can be fetched from the program by typing "Interflow_console.exe <verb> --help"
		
		Flags can either be required, optional, or in a mutually exclusive required group. For instance, if there are three flags that are in "GROUP1" then one must be used, but the other two cannot be set.
		
		List of supported verbs with their usage:
			list     - lists files and / or directories, similar to ls or dir
			download - Download file(s) from interflow
			upload   - uploads files or OneIndicators to interflow
			help     - displays this list of verbs
			version  - displays the version of the interflow console application
			
		Verb specific usage:
			list:
				-a, --apikey  | REQUIRED | Interflow API key
				-d, --dir     | REQUIRED | Remote directory to list
				-t, --type    | OPTIONAL | Filters output to only show files or directories. use f or d as arguments (ex: "--type f" )
				-v, --verbose | OPTIONAL | When used, files will be output in JSON objects with extended information.
				
				example: "Interflow_console.exe list -a xxxxxxxxxxxxxxxxxxxxxxxxxxxx --dir / --type f -v" - outputs all files in / as json objects
				
			download:
				-a, --apikey  | REQUIRED | Interflow API key
				-o, --output  | REQUIRED | Local folder to output file(s) to
				-f, --file    |  GROUP1  | File(s) to download as a space delaminated list
				-d, --dir     |  GROUP1  | Downloads all files in the specified directory
				
				example: "Interflow_console.exe download -a xxxxxxxxxxxxxxxxxxxxxxxxxxxx -o C:\Users\myusername\Desktop\ -f test2.txt test/test.txt" - Downloads the two test files to the desktop
				
			upload:
				-a, --apikey        | REQUIRED | Interflow API key
				-f, --file          |  GROUP1  | Uploads a generic file
				-j, --json          |  GROUP1  | uploads a OneIndicator definition  via a json string passed via the command line
				-i, --indicatorfile |  GROUP1  | uploads a OneIndicator definition  by specifying a file path.
				