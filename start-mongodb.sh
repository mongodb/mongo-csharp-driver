#!/bin/bash

# MongoDB Test Environment Startup Script
# Starts MongoDB with single-node replica set and test commands enabled

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored messages
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker Compose is available
if ! docker compose version &> /dev/null; then
    print_error "Docker Compose is not available. Please install Docker Compose."
    exit 1
fi

# Check if docker-compose.yml exists
if [ ! -f "docker-compose.yml" ]; then
    print_error "docker-compose.yml not found in current directory."
    exit 1
fi

print_info "Starting MongoDB with test commands enabled..."

# Stop any existing containers
docker compose down 2>/dev/null || true

# Start MongoDB
docker compose up -d

print_info "Waiting for MongoDB to be ready..."

# Wait for container to be healthy
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if docker compose ps | grep -q "healthy"; then
        print_success "MongoDB is ready!"
        break
    fi

    attempt=$((attempt + 1))
    if [ $attempt -eq $max_attempts ]; then
        print_error "MongoDB failed to start within expected time."
        print_info "Showing container logs:"
        docker compose logs
        exit 1
    fi

    echo -n "."
    sleep 2
done

echo ""

# Display connection information
print_success "MongoDB is running with the following configuration:"
echo ""
echo "  • Single-node replica set: rs0"
echo "  • Test commands: ENABLED"
echo "  • Port: 56665"
echo ""
echo "Connection String:"
echo "  mongodb://localhost:56665/?replicaSet=rs0&directConnection=true"
echo ""
echo "C# Driver Connection String:"
echo '  var connectionString = "mongodb://localhost:56665/?replicaSet=rs0&directConnection=true";'
echo '  var client = new MongoClient(connectionString);'
echo ""
print_info "To stop MongoDB, run:"
echo "  docker compose down"
echo ""
print_info "To view logs, run:"
echo "  docker compose logs -f"
echo ""
