import cmd
import os
import re
import shlex
import glob

import interflow
from pprint import pprint

### Getting Started ###
#
# Heres a small read me to help get you up and running with this code. Hopefully by reading though this you get a little bit of insight on why things were done they way they are.
# To start off, this interactive console app is based around the 'cmd' module in python. This is a great module since it deals with the tab completion and help methods for you
# however there are some querks to it. If you are looking at this code to try and understand interflow, I would recommend starting with the console app.
#
# The autocomplete method gives you 'line' and 'text' as variables. the do_* methods give you 'line' only. In the do_* methods (which run when you go to run a command), 
# the line variable is simply the non command part of the input. example: `mycommand some user/input` would yield "some user/input" in the line variable
# However, in the complete_* method you will see that line will give you the WHOLE LINE! so above, if a user was to press tab, line would yield "mycommand some user/input"
# To make matters worse, the text variable (that the cmd module docs say to use) contains a subset of the user input. it seems to only return the last part of the user input
# it seems to split on spaces and slashes, in order to only auto complete the last part the user is typing. the problem is when we are trying to build a path, we need to know
# the whole part the user tpyed in...
#
# This only gets more frustrating when you realize that users could be typing in quoted paths, or paths that contain escaped spaces. (which would cause the text variable to 
# split incorrectly. Furthermore it prevents us from using .split() to get parts of the command.
# The solution to this was to create a few helper functions. parsePathsFromFullCommandline(). It does what the name implies. it takes a full command line and parses out paths.
# The function is smart enough to detect escaped spaces and quoted strings... however as the name implies it expects the full command. in order to not rewrite it twice,
# you may see the function being used in do_* commands by simply appending the line variabe to the string of the command. There isnt anything magic happening... the command is
# junk data which simply mimics the input which is given duing the complete_* methods. Its not an optimal solution, but i figure it would keep the simplicity of this example app
# more so than creating multiple methods to essentially do the same thing.
#
#
# Other than that function, it all should be fairly strait forward. the interflow.py file (imported above) has helpful functions that wrap the API. each function returns a dictionary
# with the http response (key: 'response') and the data (key: 'data') returned. for more info on this, look at the comments in interflow.py
#
# you will also notice on most of the os.path.* calls, we do a replace afterwards to make \\ change to /. this is because linux (as well as the api) uses / but windows uses \\ and we
# want to keep this as cross platform as possible.



class Interflow(cmd.Cmd):
	"""A simple interactive interflow command line utility."""

#variables
	apiKey = ""
	validatedAPI = False
	interflowPathDir = ""

#customizations:
	#make empty lines do nothing (default repeats last command)
	def emptyline(self):
		pass

	#add a startup message and change the prompt from (cmd) to a # (until api key is set)
	intro = "Welcome to the Interflow interactive console application. Please set your api key to begin\nType help to view a list of avalible commands, and help + <command> to see command specific help\nMost commands support tab completion"
	prompt = "#"

#helper methods:
	def printSetAPI(self):
		print("*** Please run the setAPI command first before running this command")

	
	def parsePathsFromFullCommandline(self, line):
		"""a function that parses paths out of the command line. this function knows to treat quoted strings and ones with escaped spaces as single paths
			this function is also used a lot to determine how many arguments a user has entered as it knows how to treat spaces in paths.
			IMPORTANT: one weird thing in this code is that "line" in the complete* methods is the full line typed, where in the do_* methods
			    the variable line only holds the parametors being passed... so you will see this function in the do_* methods with "<command>"+line where as in the
			    complete_* methods it will just be called with line. I wish the cmd module in python was a bit more consistant with that behavior."""
		pathsOnly = line.partition(" ")[2]
		#shlex.split will remove trailing spaces... for instance "download abc/123.txt " becomes "abc/123.txt " from the partition,
		#the space on the end will be removed with the shlex below. if a user presses tab after the space, they would assume to be completing the 2nd part of the command
		#however since the space is removed the parsePathsFromFullCommandline function would only return ["abc/123.txt"] and not ["abc/123.txt",""]. thus we do this check, and add the empty
		#path on the end if needed.
		#the check for pathsOnly == "" is to catch a space after the command ('cd '), and returning the correct thing (as doing pathsOnly[-1] wound throw an index out of range error)
		if pathsOnly == "":
			return []
		if(pathsOnly[-1] == " "):
			return shlex.split(line.partition(' ')[2])+[""]
		else:
			return shlex.split(line.partition(' ')[2])
		
	def getPathFileParts(self, line, position):
		"""returns the path and file fragment at an index in the line being typed. First user entered path is at index 0. used in tab completion (fileFragment will be
			part of a file. Or can be used to break up a path a user entered for downloading a file. in this case fileFragment will be a full file name
			IMPORTANT: getPathFileParts is meant to be used in the auto completition part where line contains the command. this function can be used in do_* but you MUST
			    add some junk string with a space before the paths! (see the important note under parsePathsFromFullCommandline for more info)"""
		currentPath = self.parsePathsFromFullCommandline(line)[position]

		if currentPath == "":
			return {"path": "", "fileFragment": ""}

		#if the path doesnt start with / then we are using a relitive path and must add the current working directory to what is typed.
		#never add on the current interflow path to any path not in the 1st position... as multi position commands (download and upload) use local paths in position 2 (index 1)
		if currentPath[0] != "/" and position == 0:
			currentPath = os.path.join(self.interflowPathDir,currentPath).replace("\\","/")
		if currentPath[-1] == "/":
			#normpath takes off the end /. we dont want that...
			normalizedCurrentPath = os.path.normpath(currentPath).replace("\\","/")
			normalizedCurrentPath = normalizedCurrentPath + "/"
			path,fileFragment = os.path.split(normalizedCurrentPath)
		else:
			path,fileFragment = os.path.split(os.path.normpath(currentPath).replace("\\","/"))

		#when using os.path.normpath(), an empty path will return '.' or '/'
		#for example: os.path.normpath("test/..")  will return .
		#for example: os.path.normpath("/test/..") will return /
		#we want path to equal "" in both of these cases... so do a quick check
		if path == "." or path == "/":
			path = ""
		#same kinda thing with file fragments. this _shouldnt_ happen under normal usage, but this check stops potential unwanted behavior
		if fileFragment == ".." or fileFragment == ".":
			fileFragment = ""

		#lastly, get rid of any / in the beginning of path. this makes the different auto complete behavior on windows work, as well as making sure absolute paths dont
		#contain the / in the beginning when sending it in an api call.
		path = path.lstrip("/")
		return {"path": path, "fileFragment": fileFragment}

	def fixWindowsAutoComplete(self,path, completionResult):
		"""there seems to be a bug in pyreadline where the autocompletion return values overwrite what was written instead of appending. this is a small
		 helper function to return the full path in windows so that the returned value is correct"""
		if os.name == "nt":
			if not path:
				path = "/"
			return [os.path.join(path,result).replace("\\","/") for result in completionResult]
		else:
			return completionResult 


#########################################################
#### BEGIN CMD METHODS #################################
#######################################################


#Exit methods
	def do_EOF(self, line):
		"""Called when ctrl-D is pressed / closes out of the interactive application."""
		return True
	
	def do_exit(self, line):
		"""method that ends interactive mode"""
		return True

	def help_exit(self):
		print('\n\t'.join(["exit",
							"Exits the interactive program"]))


#Set API key methods
	def do_setAPI(self, line):
		"""Sets the api key internally for future commands."""
		if len(line.split()) != 1:
			print("There were too many arguments entered for setAPI\n")
			self.help_setAPI()
			return
				
		result = interflow.getValidation(line)
		if result["response"] == 200:
			if result["data"]["IsAuthenticated"] == True:
				print("Welcome "+result["data"]["Name"]+". Your api key is valid and ready to use.")
				#now that the user has validated their api key, change the prompt to reflect that.
				self.apiKey = line
				self.prompt = "/>"
				self.validatedAPI = True
			else:
				print("Warning: You don't appear to be an authenticated user but your API key is valid. You may need to log in to the API portal and reset your password.")
		else:
			print("*** The API key entered does not seem to be valid. Authentication check failed!")


	def help_setAPI(self):
		print('\n\t'.join(["setAPI [key]",
							"Sets the API key to be used in future commands.",
							"After calling, a check will be done to verify the api key was entered correctly"]))

#list directory commands and aliases
	def do_ls(self, line):
		"""Lists out all files and directories in the current path. equivalent to dir"""
		if not self.validatedAPI:
			self.printSetAPI()
			return
		
		if len(line) == 0:
			path=self.interflowPathDir
		else:
			path = self.getPathFileParts("ls "+line,0)["path"]

		#first get all directories from where the user is.
		results = interflow.getInterflowDirs(self.apiKey, path)
		if results["response"] == 200:
			directories = results["data"]
		else:
			print("*** Directory does not exist")
			return
		#next get all files from where the user is.
		
		#There are two end points that serve files. Download_ListFiles and File_ListSharedFiles. you have to download these files with different endpoints as well
		#If we got it from Download_ListFiles, we download with Download_File.
        #If its a file from File_ListSharedFiles it is downloaded with File_Download. 
		#
		#We only show the shared files in the root. so below is a check for if we are in the root, and if so we call the shared files instead. This check is also done when downloading
		#the files, and if they are downloading a file in the root, we call the file_download endpoint instead.
		if path == "":
			results = interflow.getInterflowSharedFileNamesOnly(self.apiKey)
		else:
			results = interflow.getInterflowFileNamesOnly(self.apiKey,path)
		
		if results["response"] == 200:
			files = results["data"]
		#now list out the results we found.
		print("Directories:")
		print("---------------------")
		print("\n".join(directories))
		print("\n")
		print("Files:")
		print("---------------------")
		print("\n".join(files))

				
	def do_dir(self, line):
		"""Lists out all files and directories in the current path. equivalent to ls"""
		self.do_ls(line)
	
	def complete_ls(self, text, line, begidx, endidx):
		"""function to complete the ls command. should return the posible directories (same as complete_cd, so this just calls complete_cd"""
		return self.complete_cd(text, line, begidx, endidx)
	
	def complete_dir(self, text, line, begidx, endidx):
		"""function to complete the dir command. should return the posible directories (same as complete_cd, so this just calls complete_cd"""
		return self.complete_cd(text, line, begidx, endidx)

	def help_ls(self):
		print('\n\t'.join(["ls <dir>",
							"Lists the contents of dir",
							"if dir not defined, lists the contents of the current directory",
							"alias - dir"]))

	def help_dir(self):
		print('\n\t'.join(["dir <dir>",
							"Lists the contents of dir",
							"if dir not defined, lists the contents of the current directory",
							"alias - ls"]))


#Change directory (cd)
	def do_cd(self, line):
		"""function to do the directory changing. handels paths with .. in them, root referenced paths, and local paths"""
		if not self.validatedAPI:
			self.printSetAPI()
			return
		
		#if the user just types cd take them back to the root
		if len(line) == 0:
			self.prompt = "/>"
			self.interflowPathDir = ""
			return

		#if the user just types "cd /". interflow doesnt return the root if you send / as a directory (it returns 404). We know it exists. so change the path internally and return
		if line.strip() == "/":
			self.prompt = "/>"
			self.interflowPathDir = ""
			return

		path = self.getPathFileParts("cd "+line,0)["path"]
		fileFragment = self.getPathFileParts("cd "+line,0)["fileFragment"]
		path = os.path.join(path,fileFragment).replace("\\","/")
		
		results = interflow.getInterflowDirs(self.apiKey, path)
		if results["response"] == 200:
			#we know the folder exists now
			self.interflowPathDir = path
			self.prompt = "/"+path+">"
		else:
			print("*** Unable to locate directory - please check your spelling and try again")

	#the cmd moduel will autocomplete "cd" but wont know what to do if someone presses tab while typing a path.
	#this method will take the current path and try to find matches and return them to the user. so fancy :)
	def complete_cd(self, text, line, begidx, endidx):
		"""function to assist autocompletion. also called by complete_ls and complete_dir"""
		#user presses <tab><tab> with nothing typed. return all directories in the current path
		if len(self.parsePathsFromFullCommandline(line)) == 0:
			pathData = {"path" : self.interflowPathDir, "fileFragment": ""}
		else:
			pathData = self.getPathFileParts(line,0)	
		
		results = interflow.getInterflowDirs(self.apiKey, pathData["path"])
		if results["response"] == 200:
			#we know the folder exists, now find all substring matches
			matches =  [found for found in results["data"] if found.startswith(pathData["fileFragment"])]
			return self.fixWindowsAutoComplete(pathData["path"],matches)

		else:
			return []
	
	def help_cd(self):
		print('\n\t'.join(["cd <dir>",
							"Changes current directory to <dir>",
							"If dir not defined, then the current directory will change to /"]))

#TODO: add support so * can download all in directory
#download file
	def do_download(self, line):

		if not self.validatedAPI:
			self.printSetAPI()
			return

		#use this instead of split as some paths might have spaces. this function expects the full command line so just add the command back on (as line is only user input)
		if len(self.parsePathsFromFullCommandline("download "+ line))!= 2:
			self.help_download()
			return

		path = self.getPathFileParts("download "+line,0)["path"]
		file = self.getPathFileParts("download "+line,0)["fileFragment"]

		#get the output file parts and glue them togeather
		outputFilePath = os.path.join(self.getPathFileParts("download "+line, 1)["path"],self.getPathFileParts("download "+line, 1)["fileFragment"]).replace("\\","/")

		#chek if the path is not "" and if it starts with a slash. the length check is to avoid index out of bounds errors.
		if len(path) and path[0] == "/":
			#internally paths do not start with a / (ex: "test/test.txt". not "/test/test.txt) so we need to strip the left most slash
			#then normalize to remove ..'s, then split into constituents
			path = path[1:]
		
		#There are two end points that serve files. Download_ListFiles and File_ListSharedFiles. you have to download these files with different endpoints as well
		#If we got it from Download_ListFiles, we download with Download_File.
        #If its a file from File_ListSharedFiles it is downloaded with File_Download. 
		#
		#We only show the shared files in the root. so below is a check for if we are in the root, and if so we call the shared files endpoint instead when downloading.
		if path == "":
			results = interflow.downloadInterflowSharedFile(self.apiKey, file)
		else:
			results = interflow.downloadInterflowFile(self.apiKey, path, file)
		if results["response"] == 200:
			try:
				if os.path.isfile(outputFilePath):
					print("*** The file already exists. Do you want to overwrite it? (y/N)")
					overwriteInput = raw_input()
					if not any(character in overwriteInput for character in ("y","Y")):
						return	
				outputFile = open(outputFilePath, 'wb')
				outputFile.write(results["data"])
			except Exception as e:
				print(e)

		else:
			print("*** Cannot open file for reading. Please check the file name given for download")

	def complete_download(self, text, line, begidx, endidx):
		"""a method to auto complete files and directories for download"""
		#complete_download is a bit different as there are two arguments it takes. the first is a remote file, and is completed though api (similar to cd and ls).
		#the difference being that it needs to return files and directories. 
		#The second part of the command is a local file or directory, and thus needs to be completed by looking at the local filesystem.

		#part 1: this handles the remote interflow file
		if len(self.parsePathsFromFullCommandline(line)) <= 1:
			if len(self.parsePathsFromFullCommandline(line)) == 0:
				pathData = {"path" : self.interflowPathDir, "fileFragment": ""}
			else:
				pathData = self.getPathFileParts(line,0)	

			results = interflow.getInterflowDirs(self.apiKey, pathData["path"])
			if results["response"] == 200:
				#we know the folder exists, now find all substring matches
				dirResults =  [found for found in results["data"] if found.startswith(pathData["fileFragment"])]
			else:
				dirResults = []
			

			#try to find files to suggest
			#like in the do_download and do_ls commands, the root contains special files shared though another endpoint. likewise, we do a check here to handle getting these files from
			#the root directory!
			if pathData["path"] == "":
				results = interflow.getInterflowSharedFileNamesOnly(self.apiKey)
			else:
				results = interflow.getInterflowFileNamesOnly(self.apiKey, pathData["path"])
				
			if results["response"] == 200:
				#we know the folder with files exists, now find all substring matches
				fileResults = [file for file in results["data"] if file.startswith(pathData["fileFragment"])]
			else:
				fileResults = []

			return self.fixWindowsAutoComplete(pathData["path"],dirResults+fileResults)
		
		#part 2: completing on the local file system
		elif len(self.parsePathsFromFullCommandline(line)) == 2:
			systemPath = os.path.join(self.getPathFileParts(line,1)["path"], self.getPathFileParts(line,1)["fileFragment"])
			if ".." not in systemPath:
				return [file for file in glob.glob(systemPath+"*/")]
			

	def help_download(self):
		print('\n\t'.join(["download [remote file] [output file]",
			"Downloads the contents of the remote file specified, and writes to [output file]",
			"Remote file can be an absolute path, or in your current directory",
			"",
			"Remote file supports full tab completion",
			"Local file supports tab completion for folders only, and will only look in the current directory forward."]))

#upload file
	def do_upload(self, line):
		if not self.validatedAPI:
			self.printSetAPI()
			return
		
		#use this instead of split as some paths might have spaces. this function expects the full command line so just add the command back on (as line is only user input)
		if len(self.parsePathsFromFullCommandline("upload "+ line))!= 2:
			self.help_upload()
			return
			
		#handle generic uploads
		if self.parsePathsFromFullCommandline("upload "+line)[0] == "generic":
			status = interflow.UploadInterflowGenericFile(self.apiKey, self.parsePathsFromFullCommandline("upload "+line)[1])
			if status == 202:
				print "The file upload sucessful"
			else:
				print "Error uploading file - " + str(status)
		
		#handle indicator uploads
		elif self.parsePathsFromFullCommandline("upload "+line)[0] == "indicator":
			with open(self.parsePathsFromFullCommandline("upload "+line)[1]) as file:
				status = interflow.UploadInterflowOneIndicator(self.apiKey, file.read())
				if status == 201:
					print "The OneIndicator upload sucessful"
				else:
					print "Error uploading OneIndicator - " + str(status)
		
		else:
			self.help_upload()
			return
		
	def complete_upload(self, text, line, begidx, endidx):
		#handle the tab completion for "generic" and "indicator"
		if len(self.parsePathsFromFullCommandline(line)) <= 1:
			#handle tab press with nothing typed
			if len(self.parsePathsFromFullCommandline(line)) == 0:
				return ["generic","indicator"]
			#get what the user has typed. there shouldnt be any escaped spaces so a split could be used, but use this to keep with what historically is used.
			fileOpetion = self.getPathFileParts(line,0)["fileFragment"]
			
			matches =  [found for found in ["generic","indicator"] if found.startswith(fileOpetion)]
			return matches
		
		#handle the tab completion for local files
		elif len(self.parsePathsFromFullCommandline(line)) == 2:
			#get the path the user has typed up to now
			Path = self.parsePathsFromFullCommandline(line)[1]

			if Path == "." or Path == "/":
				Path = ""
			results = []
			for file in glob.glob(Path+"*"):
				if os.path.isdir(file):
					results.append((os.path.split(file)[1]+"/").replace(" ","\\ "))
				else:
					results.append(os.path.split(file)[1].replace(" ","\\ "))
			return results
		
	def help_upload(self):
		print('\n\t'.join(["upload <generic|indicator> [file path]",
							"Uploads either a generic file or indicator to interflow.",
							"[file path] can be an absolute path, or in your current directory",
							"",
							"Example usage:",
							"  'upload generic somefile.txt' - uploads the generic file somefile.txt",
							"  'upload indicator someindicator.json' - uploads the OneIndicator stored in someindicator.json",
							"",
							"Upload supports full tab completion"]))
#program entry point
if __name__ == '__main__':
	try:
		import readline
	except ImportError, e:
		print "#====================================================================================#"
		print "# Welcome windows user. This program features tab completion, however to enable this #"
		print "# you must download the pyreadline library.                                          #"
		print "#                                                                                    #"
		print "# To do this, first install pip if you do not have it installed:                     #"
		print "#       https://pip.pypa.io/en/stable/installing/                                    #"
		print "# Next, from a cmd window:                                                           #"
		print "#        cd c:/Python27/scripts                                                      #"
		print "#        pip install pyreadline                                                      #"
		print "#                                                                                    #"
		print "# If you do not care about this feature, you may simply press a key and use the      #"
		print "#  program like normal, however navigating folders may be significantly more         #"
		print "#  difficult.                                                                        #"
		print "#====================================================================================#"
		if os.name == "nt":
			import msvcrt as m
			m.getch()
			os.system("cls")
		else:
			raw_input("\n\n\tCan not find a suitable readline library. Please install pyreadline though pip.\nThis program will still run, but tab completion will not be supported.\nPlease press enter to continue")
			os.system("clear") #probably on a mac that doesnt have the right version of the readline library
	try:
		import requests
	except ImportError, e:
		print "ERROR! This program requires the 'requests' library which is missing from your system, please install this then re-run the program."
		print "\tThis can be done with the following command: 'pip install requests'. For more information, please read the getting started documentation provided with this software!"
		exit()
	Interflow().cmdloop()
