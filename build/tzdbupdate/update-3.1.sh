#!/bin/bash

set -eu -o pipefail

cd $(dirname $0)

if [[ "$1" = "" ]]
then
  echo "Usage: update-3.1.sh tzdb-release-number"
  echo "e.g. update-3.1.sh 2013h"
  exit 1
fi

declare -r TZDB_RELEASE=$1
declare -r GSUTIL=gsutil.cmd
declare -r ROOT=$(realpath $(dirname $0)/../..)

rm -rf tmp-3.1
mkdir tmp-3.1
cd tmp-3.1

# Layout of tmp directory:
# - nodatime: git repo
# - old: previous zip and nupkg files
# - output: final zip and nupkg files
git clone https://github.com/nodatime/nodatime.git -b 3.1.x --depth 1
mkdir output
declare -r OUTPUT="$(realpath $PWD/output)"

# Work out the current release, fetch and extract it.
# We use "ls -l" to include the release date in the listing,
# then sed to remove the file size part, then reverse sort.
declare -r RELEASE=$(grep '<Version>' nodatime/Directory.Build.props | cut '-d>' -f 2 | cut '-d<' -f 1)
   
# Handy "increment version number" code from http://stackoverflow.com/questions/8653126
declare -r NEW_RELEASE=`echo $RELEASE | perl -pe 's/^((\d+\.)*)(\d+)(.*)$/$1.($3+1).$4/e'`

# Update the source code in the repo
cd nodatime
sed -i s/\>${RELEASE}\</\>${NEW_RELEASE}\</g Directory.Build.props
cp "${ROOT}/src/NodaTime/TimeZones/Tzdb.nzd" src/NodaTime/TimeZones

# Update the XML schema test file; this should only need to change when there's a new
# time zone.
cp "${ROOT}/src/NodaTime.Test/Xml/XmlSchemaTest.XmlSchema.approved.xml" src/NodaTime.Test/Xml

# Commit and tag the change
git commit -a -m "Update to TZDB ${TZDB_RELEASE} for release ${NEW_RELEASE}"
git tag ${NEW_RELEASE}

# Make sure the packages end up with suitable embedded paths
export ContinuousIntegrationBuild=true

# Build and package the code
echo "Packaging..."
dotnet pack -o "$OUTPUT" -c Release src/NodaTime.sln

cd ../..

echo "Done. Remaining tasks:"
echo "- Push package to nuget"
echo "- Push commit to github: git push origin 3.1.x"
echo "- Push tag to github: git push --tags origin"
