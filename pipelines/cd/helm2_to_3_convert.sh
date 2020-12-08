#!/usr/bin/env bash

set -o errexit -o pipefail

usage() {
  cat <<USAGE
$@

Usage ${script_name} [-k] [-n tiller_namespace] [-s {configmaps|secrets}]

Batch convert Helm 2 releases to Helm 3. Options:
  -k: Use kubectl to find the existing releases.

  -n: Namespace of Tiller (default "kube-system").
    Example: "-n cluster-tools"

  -s: Storage backend of Tiller. Must be either 'configmaps' or 'secrets' (default "configmaps").
    Example: "-s configmaps", "-s secrets"

  -h: Print help message.

USAGE

}

script_name="$0"
storage_backend="configmaps"
tiller_namespace="kube-system"
helm3_cmd="helm3"
user_choice="Y"

while getopts "kn:s:h" opt; do
  case "${opt}" in
    k)
      use_kubectl="true"
      ;;
    n)
      tiller_namespace="${OPTARG}"
      ;;
    s)
      storage_backend="${OPTARG}"
      ;;
    h)
      usage
      exit
      ;;
    \?)
      usage
      exit
      ;;
  esac
done


if [[ -x "$(which helm 2>/dev/null)" && -z "${use_kubectl}" ]]; then
  helm2_releases="$(helm ls --all --short)"
else
  if [[ -z "${use_kubectl}" ]]; then
    echo "'helm' is not installed or not present in PATH."
  fi
  echo "Using kubectl to get list of releases."
  echo "Using '${tiller_namespace}' namespace and '${storage_backend}' as storage backend."

  helm2_releases="$(
    kubectl get "${storage_backend}" \
      --namespace "${tiller_namespace}" \
      --selector "OWNER=TILLER" \
      --output jsonpath='{range .items[*]}{.metadata.labels.NAME}{"\n"}{end}' \
    | uniq
  )"
fi

echo -e "Found the following releases:\n${helm2_releases}\n"

for release in ${helm2_releases}; do
  ${helm3_cmd} 2to3 convert --dry-run "${release}"
  if [[ "${user_choice}" == "Y" ]]; then
    echo "Converting '${release}'."
    ${helm3_cmd} 2to3 convert "${release}"
    echo "Converted '${release}' successfully."
  else
    echo "Skipping conversion of '${release}'."
  fi
done
