"""
deploy_agent.py
Helper script to deploy agents to Azure AI Foundry from source code

Usage:
    python deploy_agent.py <agent-name> <source-directory> <runtime> <entry-point>

Example:
    python deploy_agent.py my-python-agent ./my-agent python_3_13 main.py

Environment Variables:
    AZURE_FOUNDRY_ENDPOINT - Your Azure AI Foundry project endpoint (required)
    FOUNDRY_API_URL - The API URL (default: https://localhost:5001)
    AGENT_CPU - CPU allocation (default: 2)
    AGENT_MEMORY - Memory allocation (default: 4Gi)
"""

import os
import sys
import json
import base64
import zipfile
import requests
from pathlib import Path
from typing import Optional, Dict
from datetime import datetime

class Colors:
    """ANSI color codes for terminal output"""
    RED = '\033[0;31m'
    GREEN = '\033[0;32m'
    YELLOW = '\033[1;33m'
    BLUE = '\033[0;34m'
    NC = '\033[0m'  # No Color

def print_color(message: str, color: str = Colors.NC):
    """Print colored message to terminal"""
    print(f"{color}{message}{Colors.NC}")

def print_usage():
    """Print usage information"""
    print_color("Error: Missing required arguments", Colors.RED)
    print("\nUsage: python deploy_agent.py <agent-name> <source-dir> <runtime> <entry-point>\n")
    print("Arguments:")
    print("  agent-name     - Name for the agent in Azure AI Foundry")
    print("  source-dir     - Path to the agent source code directory")
    print("  runtime        - Runtime environment (python_3_13, python_3_14, dotnet_10)")
    print("  entry-point    - Entry point file (e.g., main.py, MyAgent.dll)\n")
    print("Examples:")
    print("  python deploy_agent.py my-agent ./src python_3_13 main.py")
    print("  python deploy_agent.py data-processor ./agent dotnet_10 DataProcessor.dll\n")
    print("Required Environment Variables:")
    print("  AZURE_FOUNDRY_ENDPOINT - Your project endpoint (e.g., https://your-project.services.ai.azure.com)\n")
    print("Optional Environment Variables:")
    print("  FOUNDRY_API_URL  - API URL (default: https://localhost:5001)")
    print("  AGENT_CPU        - CPU allocation (default: 2)")
    print("  AGENT_MEMORY     - Memory allocation (default: 4Gi)")

def should_exclude(path: Path) -> bool:
    """Check if file should be excluded from zip"""
    excludes = ['__pycache__', '.pyc', '.git', 'node_modules', '.env', '.venv', 'venv', 'bin', 'obj']
    return any(ex in str(path) for ex in excludes)

def package_directory(source_dir: Path, output_zip: Path) -> None:
    """Package a directory into a zip file"""
    with zipfile.ZipFile(output_zip, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for file_path in source_dir.rglob('*'):
            if file_path.is_file() and not should_exclude(file_path):
                arcname = file_path.relative_to(source_dir)
                zipf.write(file_path, arcname)

def detect_build_command(source_dir: Path, runtime: str) -> Optional[str]:
    """Detect the appropriate build command based on runtime and files present"""
    if runtime.startswith("python"):
        if (source_dir / "requirements.txt").exists():
            return "pip install -r requirements.txt"
    elif runtime.startswith("dotnet"):
        return "dotnet restore"
    return None

def deploy_agent(
    agent_name: str,
    source_dir: Path,
    runtime: str,
    entry_point: str,
    project_endpoint: str,
    api_url: str,
    cpu: str,
    memory: str
) -> Dict:
    """Deploy an agent from source directory"""
    
    print_color("═══════════════════════════════════════════════════════════", Colors.BLUE)
    print_color("  Azure AI Foundry - Agent Deployment from Source", Colors.BLUE)
    print_color("═══════════════════════════════════════════════════════════", Colors.BLUE)
    print()
    print(f"Agent Name:       {Colors.GREEN}{agent_name}{Colors.NC}")
    print(f"Source Directory: {Colors.GREEN}{source_dir}{Colors.NC}")
    print(f"Runtime:          {Colors.GREEN}{runtime}{Colors.NC}")
    print(f"Entry Point:      {Colors.GREEN}{entry_point}{Colors.NC}")
    print(f"CPU:              {Colors.GREEN}{cpu}{Colors.NC}")
    print(f"Memory:           {Colors.GREEN}{memory}{Colors.NC}")
    print(f"Project Endpoint: {Colors.GREEN}{project_endpoint}{Colors.NC}")
    print(f"API URL:          {Colors.GREEN}{api_url}{Colors.NC}")
    print()
    
    # Step 1: Package source code
    print_color("[1/4] Packaging source code...", Colors.YELLOW)
    zip_path = Path(f"/tmp/{agent_name}.zip")
    
    if (source_dir / "requirements.txt").exists():
        print("  ✓ Found requirements.txt")
    elif list(source_dir.glob("*.csproj")):
        print("  ✓ Found .NET project file")
    
    package_directory(source_dir, zip_path)
    file_size = zip_path.stat().st_size / 1024  # KB
    print_color(f"  ✓ Created package: {file_size:.1f} KB", Colors.GREEN)
    
    # Step 2: Encode to base64
    print_color("[2/4] Encoding to base64...", Colors.YELLOW)
    with open(zip_path, 'rb') as f:
        zip_base64 = base64.b64encode(f.read()).decode('utf-8')
    
    base64_size = len(zip_base64)
    print_color(f"  ✓ Encoded {base64_size} bytes", Colors.GREEN)
    
    # Step 3: Prepare deployment request
    print_color("[3/4] Preparing deployment request...", Colors.YELLOW)
    
    build_command = detect_build_command(source_dir, runtime)
    
    payload = {
        "agentName": agent_name,
        "sourceCodeZipBase64": zip_base64,
        "runtime": runtime,
        "entryPoint": entry_point,
        "cpu": cpu,
        "memory": memory,
        "protocolVersions": [
            {"protocol": "A2A", "version": "0.2.1"}
        ],
        "environmentVariables": {
            "DEPLOYED_BY": "deploy-agent-script",
            "DEPLOYED_AT": datetime.utcnow().isoformat() + "Z"
        },
        "description": f"Deployed from source: {source_dir}"
    }
    
    if build_command:
        payload["buildCommand"] = build_command
    
    print("  ✓ Request prepared")
    
    # Step 4: Deploy to Azure AI Foundry
    print_color("[4/4] Deploying to Azure AI Foundry...", Colors.YELLOW)
    
    url = f"{api_url}/api/agents/hosted/from-source"
    params = {"projectEndpoint": project_endpoint}
    
    try:
        response = requests.post(url, json=payload, params=params)
        
        if response.status_code >= 200 and response.status_code < 300:
            print_color("✓ Deployment successful!", Colors.GREEN)
            print("\nResponse:")
            print(json.dumps(response.json(), indent=2))
            print()
            print_color("═══════════════════════════════════════════════════════════", Colors.GREEN)
            print_color(f"  Agent '{agent_name}' has been deployed successfully!", Colors.GREEN)
            print_color("═══════════════════════════════════════════════════════════", Colors.GREEN)
            print("\nNext steps:")
            print("  1. Check agent status:")
            print(f"     curl '{api_url}/api/agents/{agent_name}?projectEndpoint={project_endpoint}'")
            print("\n  2. Invoke the agent:")
            print(f"     curl -X POST '{api_url}/api/agents/{agent_name}/invoke?projectEndpoint={project_endpoint}' \\")
            print("       -H 'Content-Type: application/json' \\")
            print("       -d '{\"message\": \"Hello, agent!\"}'")
            print()
            return response.json()
        else:
            print_color("✗ Deployment failed", Colors.RED)
            print(f"\nHTTP Status Code: {response.status_code}")
            print("Response:")
            try:
                print(json.dumps(response.json(), indent=2))
            except (json.JSONDecodeError, ValueError):
                print(response.text)
            print()
            sys.exit(1)
            
    except requests.exceptions.RequestException as e:
        print_color(f"✗ Request failed: {e}", Colors.RED)
        sys.exit(1)
    finally:
        # Clean up temp file
        if zip_path.exists():
            zip_path.unlink()

def main():
    """Main entry point"""
    # Parse command line arguments
    if len(sys.argv) < 5:
        print_usage()
        sys.exit(1)
    
    agent_name = sys.argv[1]
    source_dir = Path(sys.argv[2])
    runtime = sys.argv[3]
    entry_point = sys.argv[4]
    
    # Get configuration from environment
    project_endpoint = os.getenv("AZURE_FOUNDRY_ENDPOINT")
    api_url = os.getenv("FOUNDRY_API_URL", "https://localhost:5001")
    cpu = os.getenv("AGENT_CPU", "2")
    memory = os.getenv("AGENT_MEMORY", "4Gi")
    
    # Validate
    if not project_endpoint:
        print_color("Error: AZURE_FOUNDRY_ENDPOINT environment variable is not set", Colors.RED)
        print("Set it with: export AZURE_FOUNDRY_ENDPOINT='https://your-project.services.ai.azure.com'")
        sys.exit(1)
    
    if not source_dir.is_dir():
        print_color(f"Error: Source directory '{source_dir}' does not exist", Colors.RED)
        sys.exit(1)
    
    # Deploy
    deploy_agent(
        agent_name=agent_name,
        source_dir=source_dir,
        runtime=runtime,
        entry_point=entry_point,
        project_endpoint=project_endpoint,
        api_url=api_url,
        cpu=cpu,
        memory=memory
    )

if __name__ == "__main__":
    main()
