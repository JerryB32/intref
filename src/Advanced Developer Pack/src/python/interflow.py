"""interflow API python interface. 
Created by Alexander Davis for Microsoft MSRC

-----------------
changelog:
17 jan 2017 - Created file / moved code from sample project into seperate file (this one)

"""
import sys
import json

#force requests to use tls 1.2:
from requests.adapters import HTTPAdapter
from requests.packages.urllib3.poolmanager import PoolManager
import ssl

class MyAdapter(HTTPAdapter):
    def init_poolmanager(self, connections, maxsize, block=False):
        self.poolmanager = PoolManager(num_pools=connections,
                                       maxsize=maxsize,
                                       block=block,
                                       ssl_version=ssl.PROTOCOL_TLSv1_2)

import requests
s = requests.Session()
s.mount('https://', MyAdapter)
#End requests force tls 1.2 block
									   
def getValidation(apiKey):
	"""input:
			apiKey - api key for interflow

		description:
			Checks if the given API key is valid, and if it is, returns information about the owner

		output:
			returns a dictionary:
			{
				"response": <int>,		#http response code
				"data": <dictionary>	#contents of the remote folder
			}
			
			dictionary deffinition for data
			"data": [
					{
						"Id": <string>,					#Id of user
						"FirstName": <string>,			#first name of user
						"LastName": <string>,			#last name of user
						"email": <string>,				#email of user
						"RegistrationDate": <string>,	#registration date of user",
						"AuthenticationType": <string>, #auth type of user
						"Association": <string>,		#organization of user
						"IsAuthenticated": <bool>,		#if user is authenticated
						"Name": <string>				#full name of user
					}
					]
	"""
	#validate that the api key is valid
	try:
		headers = {'Ocp-Apim-Subscription-Key': apiKey}
		
		r = requests.get('https://interflow.azure-api.net/api/debug/who', headers=headers)
		
	except Exception as e:
		sys.stderr.write(str(e))
		sys.stderr.write("\n")
	
	if r.status_code == 200:
		data = r.json()
		return {"response":200, "data":data["Identity"]}
	else:
		return {"response":r.status_code, "data":None}

def getInterflowDirs(apiKey,path):
	"""input:
			apiKey - api key for interflow
			path   - directory in interflow to to search for directories 

		description:
			Gets the list of directory names in a given directory

		output:
			returns a dictionary:
			{
				"response": <int>,		#http response code
				"data": array[<string>]	#contents of the remote folder
			}
	"""
	headers = {'Ocp-Apim-Subscription-Key': apiKey}
	params = {'directoryPath': path}

	try:
		r = requests.get("https://interflow.azure-api.net/api/downloads/directorylist", headers=headers, params=params)
	except Exception as e:
		sys.stderr.write(str(e))
		sys.stderr.write("\n")

	if r.status_code == 200:
		data = r.json()
		#append a / to denote directory
		return {"response":200, "data":[dir+"/" for dir in data]}
	else:
		return {"response":r.status_code,"data":None}

def getInterflowFiles(apiKey,path):
	"""input:
			apiKey - api key for interflow
			path   - directory in interflow to to search for files 

		description:
			Gets the list of files in a given directory

		output:
			returns a dictionary:
			{
				"response": <int>,			#http response code
				"data": array[<dictionary>]	#contents of the remote folder
			}

			dictionary deffinition for data
			"data": [
					{
						"Name": <string>,			#file name
						"Length": <int>,			#length in bytes
						"RelativePath": <string>,	#full path to file
						"LastModified": <string>	#last file modification
					}
					]
	"""
	headers = {'Ocp-Apim-Subscription-Key': apiKey}
	params = {'directoryPath': path}
	try:
		r = requests.get('https://interflow.azure-api.net/api/downloads/filelist', headers=headers, params=params)
		
	except Exception as e:
		sys.stderr.write(str(e))
		sys.stderr.write("\n")

	if r.status_code == 200:
		data = r.json()
		return {"response":200, "data":data}
	else:
		return {"response":r.status_code,"data":None}

def getInterflowFileNamesOnly(apiKey,path):
	"""input:
			apiKey - api key for interflow
			path   - directory in interflow to to search for files 

		description:
			Gets the list of file names in a given directory

		output:
			returns a dictionary:
			{
				"response": <int>,		#http response code
				"data": array[<string>]	#contents of the remote folder, names only
			}
	"""
	headers = {'Ocp-Apim-Subscription-Key': apiKey}
	params = {'directoryPath': path}
	try:
		r = requests.get('https://interflow.azure-api.net/api/downloads/filelist', headers=headers, params=params)

	except Exception as e:
		sys.stderr.write(str(e))
		sys.stderr.write("\n")

	if r.status_code == 200:
		data = r.json()
		return {"response":200, "data":[file["Name"] for file in data]}
	else:
		return {"response":r.status_code,"data":None}

def downloadInterflowFile(apiKey, path, file):
	"""input:
			apiKey - api key for interflow
			path   - directory in interflow to download newest file from
			file   - name of the file to fetch

		description:
			Gets the contents of the specified file in an interflow directory

		output:
			returns a dictionary:
			{
				"response": <int>,	#http response code
				"data": <byte>	#contents of the remote file
			}
	"""
	headers = {'Ocp-Apim-Subscription-Key': apiKey}
	params = {'directoryPath': path, 'fileName': file}

	try:
		r = requests.get("https://interflow.azure-api.net/api/downloads/file", headers=headers, params=params)
		
	except Exception as e:
		sys.stderr.write(str(e))
		sys.stderr.write("\n")
	if r.status_code == 200:
		data = r.content
		return {"response":200, "data":	data}
	else:
		return {"response":r.status_code, "data":None}

def UploadInterflowGenericFile(apiKey, filepath):
	"""
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
	"""
	headers = {'Ocp-Apim-Subscription-Key': apiKey}
	try:
		r = requests.post("https://interflow.azure-api.net/api/file/upload", files={'file': (open(filepath, 'rb'))}, headers=headers)
	except Exception as e:
		sys.stderr.write(str(e))
		sys.stderr.write("\n")
	return r.status_code
	
def UploadInterflowOneIndicator(apiKey, jsonIndicator):
	"""
		input:
			apiKey - api key for interflow
			jsonIndicator - a json array of OneIndicator definitions

		description:
			Upload OneIndicators to interflow for analysis.

		output:
			returns an int: response code from the API call.
			201 Upload sucessful.
			400 One or more indicators are not valid
			409 One or more indicators was not recieved or accepted.
			500 Internal server error"
	"""
	headers = {'Ocp-Apim-Subscription-Key': apiKey, 'Content-Type': "application/json", 'Accept-Encoding': 'UTF-8'}
	try:
		r = requests.post("https://interflow.azure-api.net/api/indicators/upload", data=jsonIndicator.encode('UTF-8'), headers=headers)
	except Exception as e:
		sys.stderr.write(str(e))
		sys.stderr.write("\n")
	return r.status_code
	
def getInterflowSharedFiles(apiKey):
	"""
		input:
			apiKey - api key for interflow

		description:
			Gets the list of shared files

		output:
			returns a dictionary:
			{
				"response": <int>,			#http response code
				"data": array[<dictionary>]	#shared file data
			}

			dictionary deffinition for data
			"data": [
					{
						"Name": <string>,			#file name
						"Length": <int>,			#length in bytes
						"RelativePath": <string>,	#full path to file
						"LastModified": <string>	#last file modification
					}
					]
    """
	headers = {'Ocp-Apim-Subscription-Key': apiKey}
	try:
		r = requests.get("https://interflow.azure-api.net/api/file/listsharedfiles", headers=headers)
		
	except Exception as e:
		sys.stderr.write(str(e))
		sys.stderr.write("\n")
	if r.status_code == 200:
		data = r.json()
		return {"response":200, "data":	data}
	else:
		return {"response":r.status_code, "data":None}

def getInterflowSharedFileNamesOnly(apiKey):
	"""
		input:
			apiKey - api key for interflow

		description:
			Gets the list of shared files

		output:
			returns a dictionary:
			{
				"response": <int>,			#http response code
				"data": array[<string>]	#file names of shared files
			}
	"""
	headers = {'Ocp-Apim-Subscription-Key': apiKey}
	try:
		r = requests.get('https://interflow.azure-api.net/api/file/listsharedfiles', headers=headers)

	except Exception as e:
		sys.stderr.write(str(e))
		sys.stderr.write("\n")

	if r.status_code == 200:
		data = r.json()
		return {"response":200, "data":[file["Name"] for file in data]}
	else:
		return {"response":r.status_code,"data":None}

def downloadInterflowSharedFile(apiKey, file):
	"""
		intput:
		apiKey - api key for interflow
		file   - name of the file to fetch

		description:
		Gets the contents of the specified shared file

		output:
		returns a dictionary:
		{
		"response": <int>,	#http response code
		"data": <byte[]>	#contents of the remote file
		}
	"""
	headers = {'Ocp-Apim-Subscription-Key' : apiKey}
	params = {'fileName': file}
	try:
		r = requests.get('https://interflow.azure-api.net/api/file/download', headers=headers, params=params)
	except Exception as e:
		sys.stderr.write(str(e))
		sys.stderr.write("\n")
	if r.status_code == 200:
		data = r.content
		return {"response":200, "data":data}
	else:
		return {"response":r.status_code,"data":None}
