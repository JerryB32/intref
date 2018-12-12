import argparse
import sys
import interflow
import os

import pprint
pp = pprint.PrettyPrinter(indent=4)

#set up argument block
global_ap = argparse.ArgumentParser(description="This console application allows for easy integration and automation between your environment and Interflow. For more detailed help, please type <subcommand> -h")

subparsers = global_ap.add_subparsers(help = 'List of avalible sub commands:', dest='subparserVar')

list_ap = subparsers.add_parser('list', help='list help')
list_ap.add_argument('-a', '--apikey', metavar='<hex string>', required=True)
list_ap.add_argument('-d', '--dir', metavar='<remote_dir>', required=True)
list_ap.add_argument('-t', '--type', choices=('f', 'd'), metavar='[f|d]',help="Select to display only files or directories")
list_ap.add_argument('-v', '--verbose',help="Displays detailed information about files in json format", action='store_true')


download_ap = subparsers.add_parser('download', help='download help')
download_ap.add_argument('-a', '--apikey', metavar='<key>', required=True)
download_ap.add_argument('-o', '--output', metavar='<local_dir>', help="Set the location to save files to", required=True)
#make -f -l and -d mutually exclusive
download_group = download_ap.add_mutually_exclusive_group(required=True)
download_group.add_argument('-f', '--file', metavar='<remote_file>', help="Downloads <file(s)> to <output>. to download whole directories use --dir", nargs="+")
download_group.add_argument('-d', '--dir', metavar='<remote_dir>', help="Downloads all files in <dir> to <output>")

upload_ap = subparsers.add_parser('upload', help='upload help')
upload_ap.add_argument('-a', '--apikey', metavar='<hex string>', required=True)
upload_group = upload_ap.add_mutually_exclusive_group(required=True)
upload_group.add_argument('-f', '--file', metavar='<file>', help="Uploads listed file(s) to Interflow", nargs="+")
upload_group.add_argument('-j', '--json', metavar='<json>', help="A json indicator to upload in OneIndicator format")
upload_group.add_argument('-i', '--indicatorfile', metavar='<file>', help="A local OneIndicator file that contains a json array of indicators.")
args = global_ap.parse_args()


###helper functions
def listDirectories(apikey, path):
	path = path.lstrip("/");
	path = path.lstrip("\\");
	
	dirResult = interflow.getInterflowDirs(apikey, path)	
	#print dirs if we got a 200 response
	if dirResult["response"] != 200:
		sys.stderr.write("There was an error getting directories in " + args.dir+"\n")
	else:
		for dir in dirResult["data"]:
			print dir


def listFiles(apikey, path, verbose):
	#remove any erroneous slashes
	path = path.lstrip("/");
	path = path.lstrip("\\");

	fileResult = interflow.getInterflowFiles(apikey, path)
	#print files if we got a 200 response
	if fileResult["response"] != 200:
		sys.stderr.write("There was an error getting files in " + args.dir+"\n")
	else:
		if not verbose:
			for file in fileResult["data"]:
				print file["Name"]
		else:
			for file in fileResult["data"]:
				pp.pprint(file)
	
	#handle shared files
	if path == "":
		sharedFiles = interflow.getInterflowSharedFiles(apikey)
		if sharedFiles["response"] != 200:
			sys.stderr.write("There was an error getting files in " + args.dir+"\n")
		else:
			if not verbose:
				for file in sharedFiles["data"]:
					print file["Name"]
			else:
				for file in sharedFiles["data"]:
					pp.pprint(file)


###CODE ENTRY POINT###
#switch statement for subparser handling:
if args.subparserVar == "list":
	if args.type == None:
		#listing both files and directories
		listDirectories(args.apikey, args.dir)
		listFiles(args.apikey, args.dir, args.verbose)
			
	elif args.type == "f":
		listFiles(args.apikey, args.dir, args.verbose)

	elif args.type == "d":
		listDirectories(args.apikey, args.dir)
		
	else:
		#user would have had to somehow mess up argparse to get here
		quit()
		
elif args.subparserVar == "download":
	#first, lets check that the output is sane.
	if args.output == "" or args.output == " ":
		sys.stderr.write("The output directory is an empty path. If you would like to output to the current directory, please use the full path or ./\n")
		quit()
	if not os.path.isdir(args.output):
		sys.stderr.write("The output directory was not found.")
		quit()

	###
	#  Start the if else tree for different file download methods
	###

	#if file(s)
	if args.file is not None:
		for file in args.file:
			file=file.lstrip("/") #incase they put a / in the beginning. common mistake i suppose.
			file=file.lstrip("\\")
			path, fileName = os.path.split(file)
			
			#shared files are put in the root (empty path). so if path is empty, use the shared file download function instead!
			if path == "":
				result = interflow.downloadInterflowSharedFile(args.apikey, fileName)
			else:
				result = interflow.downloadInterflowFile(args.apikey, path, fileName)
			
			if result["response"] == 200:
				outputFile = open(os.path.join(args.output,fileName), 'wb')
				outputFile.write(result["data"])
			else:
				sys.stderr.write("Error opening remote file "+file+" - response code: " + str(result["response"]))

	#if directory mirror
	elif args.dir is not None:
		result = interflow.getInterflowFiles(args.apikey, args.dir.lstrip("/")) #again, stripping the / incase the user included it.
		if result["response"] == 200:
			for file in result["data"]:
				path, fileName = os.path.split(file["RelativePath"])
				result = interflow.downloadInterflowFile(args.apikey,path,fileName)
				
				if result["response"] == 200:
					outputFile = open(os.path.join(args.output,fileName), 'wb')
					outputFile.write(result["data"])
				else:
					sys.stderr.write("Error opening remote file "+file)
		
	else:
		#user would have had to somehow mess up argparse to get here
		quit()

elif args.subparserVar == "upload":
	#handle generic file upload
	if args.file is not None:
		for file in args.file:
			result = interflow.UploadInterflowGenericFile(args.apikey, file)
			if result != 202:
				sys.stderr.write("Error uploading generic file "+file+" - status code: "+str(result))
		
	#handle json upload	
	elif args.json is not None:
		result = interflow.UploadInterflowOneIndicator(args.apikey, args.json)
		if result != 201:
			sys.stderr.write("Error uploading OneIndicator, status code - "+str(result))
			
	#handle file upload
	elif args.indicatorfile is not None:
		with open(args.indicatorfile) as file:
			result = interflow.UploadInterflowOneIndicator(args.apikey, file.read())
			if result != 201:
				sys.stderr.write("Error uploading OneIndicator, status code - "+str(result))
				
	else:
		#user would have had to somehow mess up argparse to get here
		quit()
	
else:
	sys.stderr.write("An unhandled exception has occured. Please verify your command and try again!")
	quit()
