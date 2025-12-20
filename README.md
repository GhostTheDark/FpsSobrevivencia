CONTEXTO GERAL

Você é um engenheiro sênior de jogos multiplayer, especialista em Unity, arquitetura cliente-servidor, FPS multiplayer, survival games e networking em tempo real.

Quero que você crie um jogo estilo Rust, totalmente multiplayer, mundo aberto, FPS, com servidor dedicado, usando Unity no Windows 11.

O projeto deve ser profissional, escalável e organizado, seguindo boas práticas de arquitetura de software, separação de responsabilidades, performance e segurança multiplayer (server authoritative).

OBJETIVO FINAL DO PROJETO

Vários jogadores conectados simultaneamente

Mundo aberto persistente

Sistema completo de sobrevivência

Construção de bases

Crafting e progressão

Combate FPS com armas

Loot, PvP, raiding

Servidor dedicado + cliente

Sincronização em tempo real

Tutorial completo passo a passo

TECNOLOGIA OBRIGATÓRIA

Engine: Unity (versão LTS compatível com Windows 11)

Linguagem: C#

Multiplayer: Arquitetura Server Authoritative

Separação:

Projeto Cliente

Projeto Servidor (headless)

Plataforma: PC (Windows 11)

ESTILO DO JOGO

FPS (First Person Shooter)

Movimentação igual ao Rust

Câmera em primeira pessoa

Armas visíveis em mãos

Mundo aberto

Jogadores reais

MECÂNICAS OBRIGATÓRIAS
🧍‍♂️ PLAYER

Movimento FPS (andar, correr, pular, agachar)

Sistema de stamina

Sistema de vida

Inventário

Equipamentos (roupa, armadura)

Temperatura corporal

Animações FPS

Respawn em saco de dormir

🌡️ SOBREVIVÊNCIA

Vida

Fome

Sede

Temperatura (frio/calor)

Radiação

Dano ambiental

Cura com itens

⛏️ COLETA

Árvores → madeira

Pedras → pedra

Plantas → tecido

Ferramentas:

Pedra

Machado

Picareta

Sistema de hitpoint nos recursos

Spawn e respawn de recursos

🏗️ CONSTRUÇÃO DE BASE

Building Plan

Martelo

Peças:

Fundação

Parede

Porta

Telhado

Upgrade de material:

Madeira

Pedra

Metal

Tool Cupboard (TC):

Área de controle

Sistema de upkeep

Decay da base

🎒 INVENTÁRIO & CRAFTING

Inventário grid

Stack de itens

Sistema de crafting

Tempo de craft

Bancadas:

Tier 1

Tier 2

Tier 3

Scrap

Blueprints

Research Table

Tech Tree

🔫 COMBATE

Armas primitivas:

Lança

Arco

Armas de fogo:

Pistola

Rifle

Sistema FPS:

Raycast

Hitbox por parte do corpo

Tipos de dano:

Balístico

Corte

Explosão

Armaduras com proteção por parte do corpo

Recoil

Reload

Munição

🧠 MULTIPLAYER

Servidor autoritativo

Sincronização:

Movimento

Vida

Ações

Anti-cheat básico

Spawn de jogadores

Desconexão e reconexão

Persistência de dados

Chat

🗺️ MUNDO

Mapa procedural

Biomas

Monumentos

Zonas com radiação

Safe Zones:

Outpost

Bandit Camp

Loot spawnado por área

ESTRUTURA DE PROJETO (OBRIGATÓRIA)

Você DEVE criar e explicar TODOS os arquivos abaixo:

📁 PASTAS
Assets/
 ├── Scripts/
 │   ├── Core/
 │   ├── Networking/
 │   ├── Player/
 │   ├── Survival/
 │   ├── Inventory/
 │   ├── Crafting/
 │   ├── Building/
 │   ├── Combat/
 │   ├── World/
 │   ├── UI/
 │   ├── Utils/
 │
 ├── Prefabs/
 │   ├── Player/
 │   ├── Weapons/
 │   ├── Buildings/
 │   ├── Items/
 │
 ├── Scenes/
 ├── Materials/
 ├── Animations/
 ├── UI/
 └── Resources/

SCRIPTS OBRIGATÓRIOS (VOCÊ DEVE CRIAR TODOS)
🔌 NETWORK

NetworkManager

ServerBootstrap

ClientBootstrap

NetworkPlayer

NetworkTransform

NetworkCombat

NetworkInventory

NetworkBuilding

👤 PLAYER

PlayerController

PlayerMotor

PlayerCamera

PlayerStats

PlayerHealth

PlayerHunger

PlayerThirst

PlayerTemperature

PlayerStamina

🎒 INVENTÁRIO

InventorySystem

InventorySlot

ItemData

ItemDatabase

LootContainer

🏗️ CONSTRUÇÃO

BuildingSystem

BuildingGhost

BuildingPiece

ToolCupboard

BaseDecaySystem

🔫 COMBATE

WeaponBase

GunWeapon

MeleeWeapon

Projectile

DamageSystem

Hitbox

🧠 SERVER

ServerWorldManager

ServerPlayerManager

ServerSaveSystem

ServerLootSpawner

ServerAIManager

🖥️ UI

HUDManager

InventoryUI

CraftingUI

BuildingUI

StatusBarsUI

CrosshairUI

O QUE VOCÊ DEVE FAZER NA RESPOSTA

Criar TODOS os scripts

Explicar cada script em detalhes

Mostrar exemplos de código

Explicar como tudo se conecta

Criar um tutorial PASSO A PASSO, incluindo:

Instalar Unity

Criar projeto

Configurar multiplayer

Criar servidor

Build servidor

Rodar servidor

Conectar cliente

Explicar:

Como testar multiplayer local

Como testar com dois PCs

Como rodar servidor dedicado

Explicar boas práticas

Explicar possíveis melhorias futuras

REGRAS IMPORTANTES

Código limpo

Arquitetura modular

Comentários no código

Pensar em performance

Pensar em segurança multiplayer

NÃO simplificar

NÃO pular etapas

NÃO usar soluções genéricas

RESULTADO ESPERADO

Ao final, qualquer desenvolvedor iniciante deve conseguir:

✅ Abrir a Unity
✅ Criar o projeto
✅ Rodar o servidor
✅ Conectar múltiplos jogadores
✅ Atirar
✅ Tomar dano
✅ Coletar recursos
✅ Construir base
✅ Craftar itens
✅ Jogar em tempo real

Comece agora.
