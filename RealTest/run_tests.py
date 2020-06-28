#!/usr/bin/env python3
import subprocess

subprocess.run(["docker", "build", "-f", "docker/server/Dockerfile", "..", "-t", "bsu_server"])
subprocess.run(["docker", "build", "-f", "docker/client/Dockerfile", "..", "-t", "bsu_client"])

subprocess.run(["docker", "network", "create", "-d", "bridge", "bsu-net"])

subprocess.run(["docker", "run", "--rm", "-d", "--network=bsu-net", "--network-alias=server", "--name=bsu_server_1", "bsu_server"])
test_cases = subprocess.check_output(["docker", "run", "--rm", "--network=bsu-net", "bsu_client", "get"])
test_cases = test_cases.decode().strip().split("\n")

print(test_cases)

for test_case in test_cases:
    print("TEST CASE: " +test_case)
    subprocess.run(["docker", "run", "--rm", "--network=bsu-net", "bsu_client", test_case])

subprocess.run(["docker", "stop", "bsu_server_1"])
subprocess.run(["docker", "network", "rm", "bsu-net"])
