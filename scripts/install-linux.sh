#!/bin/bash
set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}SaveStateReborn Linux Installer${NC}"
echo "================================"
echo ""

# Detect distribution
if [ -f /etc/os-release ]; then
    . /etc/os-release
    DISTRO=$ID
else
    echo -e "${RED}Cannot detect distribution${NC}"
    exit 1
fi

echo "Detected distribution: $DISTRO"

# Check for dotnet
if ! command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}.NET SDK not found. Installing...${NC}"
    
    case $DISTRO in
        ubuntu|debian)
            wget https://packages.microsoft.com/config/$DISTRO/$VERSION_ID/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            sudo apt-get update
            sudo apt-get install -y dotnet-sdk-9.0
            ;;
        fedora)
            sudo dnf install -y dotnet-sdk-9.0
            ;;
        arch|manjaro)
            sudo pacman -S --noconfirm dotnet-sdk
            ;;
        *)
            echo -e "${RED}Automatic installation not supported for $DISTRO${NC}"
            echo "Please install .NET 9.0 SDK manually"
            exit 1
            ;;
    esac
fi

# Install dependencies
echo -e "${YELLOW}Installing dependencies...${NC}"
case $DISTRO in
    ubuntu|debian)
        sudo apt-get install -y libgtk-3-dev libssl-dev libicu-dev
        ;;
    fedora)
        sudo dnf install -y gtk3-devel openssl-devel libicu-devel
        ;;
    arch|manjaro)
        sudo pacman -S --noconfirm gtk3 openssl icu
        ;;
esac

# Clone and build
echo -e "${YELLOW}Building SaveStateReborn...${NC}"
INSTALL_DIR="${HOME}/.local/share/SaveStateReborn"
mkdir -p "$INSTALL_DIR"

if [ ! -d "$INSTALL_DIR/source" ]; then
    git clone https://github.com/DocDamage/Save_State-Gaming-Hub.git "$INSTALL_DIR/source"
fi

cd "$INSTALL_DIR/source"
dotnet build src/SaveState.Presentation -c Release

# Create launcher
echo -e "${YELLOW}Creating launcher...${NC}"
cat > "$HOME/.local/bin/savestate" << 'EOF'
#!/bin/bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1
cd "$HOME/.local/share/SaveStateReborn/source"
dotnet run --project src/SaveState.Presentation -c Release -- "$@"
EOF

chmod +x "$HOME/.local/bin/savestate"

# Add to PATH if needed
if [[ ":$PATH:" != *":$HOME/.local/bin:"* ]]; then
    echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.bashrc"
    echo -e "${YELLOW}Please run: source ~/.bashrc${NC}"
fi

# Set capabilities for memory reading
echo -e "${YELLOW}Setting up memory reading permissions...${NC}"
if command -v setcap &> /dev/null; then
    sudo setcap cap_sys_ptrace=eip "$HOME/.local/bin/savestate" 2>/dev/null || true
fi

# Desktop entry
echo -e "${YELLOW}Creating desktop entry...${NC}"
mkdir -p "$HOME/.local/share/applications"
cat > "$HOME/.local/share/applications/savestatereborn.desktop" << EOF
[Desktop Entry]
Name=SaveState Reborn
Comment=Gaming Management Platform
Exec=$HOME/.local/bin/savestate
Icon=$INSTALL_DIR/source/assets/icon.png
Type=Application
Categories=Game;Utility;
EOF

echo ""
echo -e "${GREEN}Installation complete!${NC}"
echo ""
echo "Usage:"
echo "  savestate              # Launch application"
echo "  savestate --help       # Show help"
echo ""
echo "For Steam Deck users, see: docs/guides/STEAM_DECK.md"
