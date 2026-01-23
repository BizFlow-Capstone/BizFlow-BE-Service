#!/bin/bash
echo "Running database migration..."
docker exec -i bizflow-mysql mysql -ubizflow_user -pbizflow_pass_123 bizflow_db < $1
echo "Migration completed!"