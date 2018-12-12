Getting python / pip set up:
	Pip is the most commonly used python package manager, and is required to install packages needed to run the python scripts.
	
	Windows:
		Windows does not ship with python, so it must be downloaded first (https://www.python.org/downloads/windows/)
		
		The current latest version of python 2.7 is 2.7.13. Please install the newest version of python 2.7
		
		Pip should have been installed with python. Navigate to 'C:\Python27\Scripts' and see if pip.exe exists in the directory. If it does not, please follow this guide: https://pip.pypa.io/en/stable/installing/
		
		When installing modules with pip, open a terminal in the 'C:\Python27\Scripts' directory and run "pip.exe install <module>"
		
		Lastly, an optional step which makes using python easier: to launch python applications, you normally have to type the full path to the python executable followed by the script you wish to run.
		If instead you want to be able to just type "python <script>" no matter your directory, you need to add the python executable to your system path.
			
			open your start menu and type "advanced system settings" (or My Computer > Properties > Advanced System Settings )
			click "Environment Variables..."
			click new under system variables. Make sure you click the system variable button and not the user variables button.
			use the following settings:
				Variable name:  PYTHONPATH
				Variable value: C:\Python27;C:\Python27\Lib;C:\Python27\DLLs;C:\Python27\Lib\lib-tk;C:\Python27\Scripts
		
			If you ran the optional step, you can run python / pip like normal without needing to specify the path / extension.
		
	OSX:
		Python comes pre-installed with OSX, however pip does not.
		
		To install pip, open terminal and type "sudo easy_install pip"
		
	Linux:
		Most linux distros will ship with python, however since there is a large variety in linux distributions, you may not have python or pip.
		
		To check if you have python, type "python -V" you should see python 2.7.* returned. If you see python 3.*.*, try again with "python2 -V". if this is successful, run all future commands as "python2 <command>" and "pip2 <command>"
		
		If python is not installed, use your default package manager to install it. for debian / ubuntu based distros, type "sudo apt-get install python2.7 python-pip python-dev build-essential" This will install python as well as pip.
		
		If you are on an older version of ubuntu (before 10.10), or if the above command failed. type "sudo apt-get install python2.7 python-setuptools python-dev build-essential " followed by "sudo easy_install pip"
		
		Verify python was installed with the python -V command discussed earlier.
		
Dependencies:

	console_interflow.py:
		requests                             can be installed with "pip install requests"
	
	interactive_interflow.py:
		requests                             can be installed with "pip install requests"
		if running windows:
			(optional) pyreadline            can be installed with "pip install pyreadline" (enables tab completion)
		
Notes:
	On windows, interactive_interflow.py requires the pyreadline module for tab completion to work. If this is not installed, a warning will pop up, however pressing any key will continue to the program with the feature disabled.
	
	On OSX, tab completion will not work with interactive_interflow.
		
		
		
		
		
Using:

	interactive_interflow.py:
		When launching the application, you will notice the prompt cursor is a '#'. This indicates that an API key is not set. To begin using the program, first type "setAPI <apikey>"
		
		List of commands:
			help <cmd>                              - displays a list of supported commands. use help <command> for detailed help on the command.
			cd [dir]                                - changes directory to the specified path. use ../ to go back a directory
			dir <dir>                               - displays the content of the current directory. optionally specify a path to display the contents of the specified directory.
			download [remote file] [output file]    - downloads a file from interflow
			upload [generic|indicator] [file path]  - uploads a OneIndicator by specifying a file path.
		
		Upon running setAPI, the program will validate the api key. If the key is valid, then you will be greeted and the "#" will change to "/>"
		The part of the prompt before the ">" character is the current file path. You can use cd to change your directory, and ls or dir to output the file and folder structure.
		
		All commands implement tab completion. You can use this to more easily select files for downloading, or to navigate the directories. 
		Pressing tab a single time will autocomplete as much as possible.
		Pressing tab twice in rapid succession  will display all potential files that match. These two features can be combined to quickly type long file names.
		*NOTE*: each tab press requires an api call. Sometimes it may take time to return data. Pressing tab repeatedly will not make this happen  any quicker!
		
		
	
	console_interflow.py:
		This application uses verb commands, similar to git. Each verb acts like its own program with its own set of flags which can be used.
		All flags have a short hand and long hand reference; all short hand flags are denoted with a signle dash and a letter (-h) where long hand commands use two dashes (--help)
		
		This usage help can be fetched from the program by typing "Interflow_console.exe <verb> --help"
		
		Flags can either be required, optional, or in a mutually exclusive required group. For instance, if there are three flags that are in "GROUP1" then one must be used, but the other two cannot be set.
		
		List of supported verbs with their usage:
			list     - lists files and / or directories, similar to ls or dir
			download - Download file(s) from interflow
			upload   - uploads files or OneIndicators to interflow
			help     - displays this list of verbs

			
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
				