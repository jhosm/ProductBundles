#!/bin/bash

# Setup MongoDB Docker container for ProductBundles
echo "Setting up MongoDB Docker container for ProductBundles..."

# Start MongoDB container
docker run -d --name productbundles-mongodb \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=ProductBundles123! \
  -v mongodb_data:/data/db \
  --restart unless-stopped \
  mongo:latest

# Wait for MongoDB to be ready
echo "Waiting for MongoDB to start..."
sleep 15

# Check if MongoDB is ready
echo "Checking MongoDB connection..."
docker exec productbundles-mongodb mongosh --eval "db.adminCommand('ping')" --quiet

if [ $? -eq 0 ]; then
    echo "✅ MongoDB is ready!"
    echo ""
    echo "Connection Details:"
    echo "  Server: localhost:27017"
    echo "  Username: admin"
    echo "  Password: ProductBundles123!"
    echo "  Connection String: mongodb://admin:ProductBundles123!@localhost:27017"
    echo ""
    echo "To stop the container: docker stop productbundles-mongodb"
    echo "To remove the container: docker rm productbundles-mongodb"
    echo "To view logs: docker logs productbundles-mongodb"
else
    echo "❌ MongoDB failed to start properly"
    echo "Check logs with: docker logs productbundles-mongodb"
fi
