#!/bin/bash
echo "Removing jube-cluster..."

output=$(docker stack rm jube-cluster 2>&1)

if echo "$output" | grep -q "Nothing found in stack: jube-cluster"; then
  echo "Stack 'jube-cluster' not found. Exiting."
  exit 0
fi
sleep 15
echo "Done."