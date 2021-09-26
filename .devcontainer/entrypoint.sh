#!/bin/sh

echo Running entrypoint.sh

#################################################################################################################
# Provided the Dockerfile doesn't change the user, this script will run as 'root'. However, once VS Code connects
# it will connect remotely as user 'dev' [Manfred, 19sep2021]


#################################################################################################################
# Set version to be shared by all csproj files:
chmod +x /src/.devcontainer/setversion.ps1
# Option '+x' adds execute permission for the file


#################################################################################################################
# Change ownership of all directories and files in the mounted volume:
chown -R dev:dev /src
# Option '-R' applies the ownerhip change recursively on files and directories in /src


#################################################################################################################
# Retrieve the self-signed SSL certificate of the CosmosDB Emulator and install in dev container
#
echo Retrieving self-signed SSL certificate from CosmosDB Emulator
# Ref: https://docs.microsoft.com/en-us/azure/cosmos-db/linux-emulator?tabs=ssl-netstd21#run-on-linux
retry=1
while [ $retry -lt 21 ]
do
   # Use wget instead of curl as it is more reliable in terms of error handling. [Manfred, 22sep2021]
   wget --no-check-certificate --output-document=/tmp/emulator.crt https://demo-database.local:8081/_explorer/emulator.pem
   if [ "$?" -eq 0 ]
   then
      echo "wget successful"
      break
   else
      echo "******* Waiting for retry" $retry "*******"
      sleep 5
   fi
   retry=`expr $retry + 1`
done
# Copy to well-known location
cp /tmp/emulator.crt /usr/local/share/ca-certificates
# Remove symbol link to trigger update of ca-certificates.crt file. 
# See http://manpages.ubuntu.com/manpages/xenial/man8/update-ca-certificates.8.html
rm -rf /etc/ssl/certs/emulator.pem 
# Trigger update of file with concatenated list of certificates:
update-ca-certificates > /tmp/update-ca-certificates-result.txt
# To check if the previous result was successful, check content of file /tmp/update-ca-certificates-result.txt
#
# To confirm the certificate was correctly installed, use the following command from inside the dev container. 
# Note that it doesn't use option '--no-check-certificate' which means, if successful, it used the certificate
# that was just installed:
#    wget https://demo-database.local:8081/_explorer/emulator.pem
#
# To check certificate DNS entries in the self-signed certificate with the following command
# openssl x509 -noout -text -in /tmp/emulator.crt


#################################################################################################################
# Finally invoke what has been specified as CMD in Dockerfile or command in docker-compose:
"$@"
