#!/usr/bin/env bash

#
# FeatureTestDynamic.sh - Beispiel-Skript für dynamische Tokens
# Monster Trading Cards Game
#

echo "=== MTCG - cURL Integration Test (Dynamische Tokens) ==="

# -------------------------------------
# Prüfen, ob "pause" als Parameter übergeben wurde
pauseFlag=0
for arg in "$@"; do
  if [ "$arg" == "pause" ]; then
    pauseFlag=1
    break
  fi
done

function pause() {
  if [ $pauseFlag -eq 1 ]; then
    read -p "Press Enter to continue..."
  fi
}

SERVER_URL="http://localhost:10001"

# -------------------------------------
# 1) CREATE USERS
echo
echo "=== 1) Create Users (Register) ==="
curl -i -X POST "$SERVER_URL/users" \
     -H "Content-Type: application/json" \
     -d '{"Username": "kienboec", "Password": "daniel"}'
echo
curl -i -X POST "$SERVER_URL/users" \
     -H "Content-Type: application/json" \
     -d '{"Username": "altenhof", "Password": "markus"}'
echo
curl -i -X POST "$SERVER_URL/users" \
     -H "Content-Type: application/json" \
     -d '{"Username": "admin", "Password": "istrator"}'
echo
echo "(Erwartet: 201, falls Benutzer noch nicht existierten)"
pause

# -------------------------------------
# 2) LOGIN USERS & PARSE TOKENS
echo
echo "=== 2) Login Users (Sessions) ==="

# --- Kienboec ---
LOGIN_RESPONSE_KIENBOEC=$(curl -s -X POST "$SERVER_URL/sessions" \
  -H "Content-Type: application/json" \
  -d '{"Username": "kienboec", "Password": "daniel"}')

KIENBOEC_TOKEN=$(echo "$LOGIN_RESPONSE_KIENBOEC" | jq -r '.Token')
echo "Login kienboec => Token: $KIENBOEC_TOKEN"

# --- Altenhof ---
LOGIN_RESPONSE_ALTENHOF=$(curl -s -X POST "$SERVER_URL/sessions" \
  -H "Content-Type: application/json" \
  -d '{"Username": "altenhof", "Password": "markus"}')

ALTENHOF_TOKEN=$(echo "$LOGIN_RESPONSE_ALTENHOF" | jq -r '.Token')
echo "Login altenhof => Token: $ALTENHOF_TOKEN"

# --- Admin ---
LOGIN_RESPONSE_ADMIN=$(curl -s -X POST "$SERVER_URL/sessions" \
  -H "Content-Type: application/json" \
  -d '{"Username": "admin", "Password": "istrator"}')

ADMIN_TOKEN=$(echo "$LOGIN_RESPONSE_ADMIN" | jq -r '.Token')
echo "Login admin => Token: $ADMIN_TOKEN"

echo
echo "(Erwartet: Gültige Tokens als GUID o.Ä.)"
pause

# -------------------------------------
# 3) USER Kienboec kauft ein Package
echo
echo "=== 3) Buy a Package (Kienboec) ==="
curl -i -X POST "$SERVER_URL/packages" \
     -H "Authorization: Bearer $KIENBOEC_TOKEN" \
     -H "Content-Type: application/json" \
     -d ''
echo
echo "(Erwartet: 200 oder 201, falls Kauf erfolgreich)"
pause

# -------------------------------------
# 4) LIST CARDS (Kienboec)
echo
echo "=== 4) List Own Cards (Kienboec) ==="
curl -i -X GET "$SERVER_URL/cards" \
     -H "Authorization: Bearer $KIENBOEC_TOKEN"
echo
echo "(Erwartet: 200 und JSON-Array mit Kienboecs Karten)"
pause

# -------------------------------------
# 5) CONFIGURE DECK
echo
echo "=== 5) Configure Deck (Kienboec) ==="
# Beispiel mit 4 int-IDs oder GUIDs. Passe an deine IDs an!
curl -i -X POST "$SERVER_URL/decks" \
     -H "Authorization: Bearer $KIENBOEC_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"CardIds": [704, 705, 706, 707]}'
echo
echo "(Erwartet: 200 OK, Deck gespeichert)"
pause

# -------------------------------------
# 6) SHOW DECK
echo
echo "=== 6) Show Deck (Kienboec) ==="
curl -i -X GET "$SERVER_URL/decks" \
     -H "Authorization: Bearer $KIENBOEC_TOKEN"
echo
pause

# -------------------------------------
# 7) START A BATTLE
echo
echo "=== 7) Start a Battle (Kienboec) ==="
curl -i -X POST "$SERVER_URL/battles" \
     -H "Authorization: Bearer $KIENBOEC_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"Username": "kienboec"}'
echo
echo "(Erwartet: 201 Created mit Battle-Infos)"
pause

# -------------------------------------
# 8) SCOREBOARD
echo
echo "=== 8) Show Scoreboard ==="
curl -i -X GET "$SERVER_URL/scoreboard" \
     -H "Authorization: Bearer $KIENBOEC_TOKEN"
echo
pause

# -------------------------------------
# 9) CLAIM POWERUP
echo
echo "=== 9) Claim a PowerUp (Kienboec) ==="
curl -i -X POST "$SERVER_URL/powerups/claim" \
     -H "Authorization: Bearer $KIENBOEC_TOKEN"
echo
pause

# -------------------------------------
# 10) LIST POWERUPS
echo
echo "=== 10) List PowerUps (Kienboec) ==="
curl -i -X GET "$SERVER_URL/powerups" \
     -H "Authorization: Bearer $KIENBOEC_TOKEN"
echo
pause

# -------------------------------------
# 11) CREATE TRADE
echo
echo "=== 11) Create Trade (Kienboec) ==="
curl -i -X POST "$SERVER_URL/trades" \
     -H "Authorization: Bearer $KIENBOEC_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
          "OfferedCardId": 704,
          "RequirementType": "spell",
          "RequirementElement": "fire",
          "RequirementMinDamage": 30
        }'
echo
pause

# -------------------------------------
# 12) GET ACTIVE TRADES
echo
echo "=== 12) List all Trades (Kienboec) ==="
curl -i -X GET "$SERVER_URL/trades" \
     -H "Authorization: Bearer $KIENBOEC_TOKEN"
echo
pause

echo
echo "=== Done. ==="

