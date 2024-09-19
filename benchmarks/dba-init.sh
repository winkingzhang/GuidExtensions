#!/usr/bin/env sh

# Run this script to initialize database before benchmark
# NOTE:
# for testing propose, the database is not persistent, 
# it will be removed after container is stopped
docker run --rm \
    --name=uudi-test-postgres \
    -e POSTGRES_USER=tmpdba \
    -e POSTGRES_PASSWORD=P@assword \
    -e POSTGRES_DB=testdb \
    -e DB_PORT=5432 \
    -p 5432:5432 \
    postgres:15.8 \
        -c shared_buffers=256MB \
        -c max_connections=200