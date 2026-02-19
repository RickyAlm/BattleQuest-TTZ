# BattleQuest - API de Análise de Logs

Uma API RESTful completa para processamento, armazenamento e análise de eventos em larga escala do jogo BattleQuest. O sistema processa centenas de milhares de eventos de logs, permitindo consultas avançadas, dashboards analíticos e insights sobre o comportamento dos jogadores.

## 📋 Índice

- [Tecnologias Utilizadas](#-tecnologias-utilizadas)
- [Pré-requisitos](#-pré-requisitos)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Guia de Instalação](#-guia-de-instalação)
- [Importação de Logs](#-importação-de-logs)
- [Executando o Projeto](#-executando-o-projeto)
- [Testes](#-testes)
- [Documentação da API](#-documentação-da-api)
- [Recursos e Funcionalidades](#-recursos-e-funcionalidades)
- [Licença](#-licença)

---

## 🚀 Tecnologias Utilizadas

### Backend
- **.NET 8.0** - Framework principal
- **ASP.NET Core** - API RESTful
- **Entity Framework Core 8.0.24** - ORM
- **PostgreSQL 16** - Banco de dados relacional
- **Npgsql** - Driver PostgreSQL para .NET

### Arquitetura e Padrões
- **Clean Architecture** - Separação em camadas (Domain, Application, Infrastructure, API)
- **Repository Pattern** - Abstração de acesso a dados
- **Dependency Injection** - Inversão de controle
- **CQRS Pattern** - Separação de comandos e consultas

### Documentação e Testes
- **Swagger/OpenAPI** - Documentação interativa da API
- **xUnit** - Framework de testes unitários
- **Moq** - Biblioteca de mocking
- **Postman Collection** - Documentação e testes da API
- **Drawio** - Diagrama do banco de dados

### DevOps
- **Docker & Docker Compose** - Containerização do banco de dados
- **Git** - Controle de versão

---

## 📦 Pré-requisitos

Antes de começar, certifique-se de ter instalado em sua máquina:

### 1. .NET 8.0 SDK

**Download:** [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)

**Verificar instalação:**

Após a instalação, abra um terminal e execute:

```bash
dotnet --version
```

Deve retornar: `8.0.x` ou superior

### 2. Docker Desktop

**Windows/Mac:** [https://www.docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)

**Após a instalação:**

1. Abra o **Docker Desktop**
2. No primeiro acesso, pode ser solicitado atualizar o WSL. Execute no terminal:

```bash
wsl --update
```

3. Aguarde o Docker Desktop inicializar completamente

**Verificar instalação:**

Abra um terminal e execute:

```bash
docker --version
docker-compose --version
```

### 3. Visual Studio 2026 ou 2022

**Download:** [https://visualstudio.microsoft.com/downloads/](https://visualstudio.microsoft.com/downloads/)

**Durante a instalação, selecione os seguintes workloads:**
- ✅ **ASP.NET e Desenvolvimento Web**
- ✅ **Desenvolvimento para Desktop com .NET**

> **💡 Importante:** Esses workloads são necessários para compilar e executar o projeto corretamente.

### 4. Git

**Download:** [https://git-scm.com/downloads](https://git-scm.com/downloads)

---

## 📁 Estrutura do Projeto

```
BattleQuest-TTZ/
│
├── BattleQuest.slnx                        # Solução principal do .NET (todos os projetos)
│
├── docs/                                   # Documentação do projeto
│   ├── database/
│   │   ├── battlequest_diagram.png         # Imagem do diagrama do banco
│   │   ├── BattleQuestDiagram.drawio       # Diagrama ER editável do banco de dados
│   │   └── initial.sql                     # Script SQL inicial
│   └── postman/
│       └── BattleQuest_APIv1.postman_collection.json  # Coleção Postman completa
│
├── src/                                    # Código fonte principal
│   │
│   ├── BattleQuest.API/                    # Camada de apresentação (API RESTful)
│   │   ├── Controllers/                    # Endpoints da API
│   │   │   ├── DashboardController.cs      # Métricas consolidadas
│   │   │   ├── EventsController.cs         # Consulta de eventos
│   │   │   ├── ItemsController.cs          # Estatísticas de itens
│   │   │   ├── LeaderboardController.cs    # Ranking de jogadores
│   │   │   ├── PlayersController.cs        # Dados dos jogadores
│   │   │   └── HealthController.cs         # Health check
│   │   ├── Filters/
│   │   │   └── DatabaseExceptionFilter.cs  # Tratamento global de erros
│   │   ├── Security/
│   │   │   └── ApiTokenAuthenticationHandler.cs  # Autenticação via token
│   │   └── Program.cs                      # Configuração da aplicação
│   │
│   ├── BattleQuest.Application/            # Camada de aplicação (Use Cases)
│   │   ├── Import/                         # Pipeline de importação de logs
│   │   │   ├── GameLogImportPipeline.cs
│   │   │   ├── Parsing/                    # Parsers de logs
│   │   │   ├── Mapping/                    # Mapeamento de dados
│   │   │   └── Ports/                      # Interfaces de adaptadores
│   │   └── Queries/                        # Queries de leitura (CQRS)
│   │       ├── Dashboard/                  # DTOs e interfaces do dashboard
│   │       ├── Events/                     # DTOs e interfaces de eventos
│   │       ├── Items/                      # DTOs e interfaces de itens
│   │       ├── Leaderboard/                # DTOs e interfaces do ranking
│   │       └── Players/                    # DTOs e interfaces de jogadores
│   │
│   ├── BattleQuest.Domain/                 # Camada de domínio (Entidades)
│   │   └── Entities/
│   │       ├── Player.cs                   # Entidade Jogador
│   │       ├── Event.cs                    # Entidade Evento
│   │       ├── Item.cs                     # Entidade Item
│   │       ├── Boss.cs                     # Entidade Chefe
│   │       ├── Quest.cs                    # Entidade Missão
│   │       ├── Zone.cs                     # Entidade Zona
│   │       ├── Channel.cs                  # Entidade Canal
│   │       └── ActionType.cs               # Entidade ActionType
│   │
│   ├── BattleQuest.Infrastructure/         # Camada de infraestrutura
│   │   ├── Database/
│   │   │   ├── BattleQuestDbContext.cs     # Contexto do EF Core
│   │   │   ├── Configurations/             # Configurações de entidades
│   │   │   └── Migrations/                 # Migrações do banco
│   │   ├── Import/                         # Implementações de importação
│   │   │   ├── Lookup/                     # Serviços de lookup/cache
│   │   │   ├── Persistence/                # Repositórios
│   │   │   └── Upsert/                     # Serviços de insert/update
│   │   └── Queries/                        # Implementações de queries
│   │       ├── EfDashboardQueries.cs
│   │       ├── EfEventQueries.cs
│   │       ├── EfItemQueries.cs
│   │       ├── EfLeaderboardQueries.cs
│   │       └── EfPlayerQueries.cs
│   │
│   └── BattleQuest.Importer/               # Aplicação console para importação
│       ├── Program.cs                      # Ponto de entrada do importador
│       ├── FileLineReader.cs               # Leitor de arquivos de log
│       └── Data/
│           ├── game_log_small.txt          # Log de exemplo (6.000 eventos)
│           └── game_log_large.txt          # Log completo (126.017 eventos)
│
├── tests/                                  # Projetos de testes
│   │
│   ├── BattleQuest.API.Tests/              # Testes dos controllers
│   │   ├── Controllers/                    # Testes unitários dos endpoints
│   │   └── Security/                       # Testes de autenticação
│   │
│   ├── BattleQuest.Application.Tests/      # Testes da camada de aplicação
│   │   ├── Import/                         # Testes do pipeline de importação
│   │   │   ├── Parsing/                    # Testes dos parsers
│   │   │   └── Mapping/                    # Testes dos mapeadores
│   │   ├── Fakes/                          # Mocks e stubs
│   │   └── TestHelpers/                    # Utilitários de teste
│   │
│   └── BattleQuest.Integration.Tests/      # Testes de integração
│       ├── Import/                         # Testes com banco real
│       └── Infrastructure/                 # Fixtures de banco de dados
│
├── docker-compose.yml                      # Configuração do PostgreSQL
├── .gitignore
├── LICENSE
└── README.md
```

---

## 🔧 Guia de Instalação

### Passo 1: Clonar o Repositório

Abra um terminal (PowerShell, CMD ou Git Bash) em qualquer local e execute:

```bash
git clone https://github.com/RickyAlm/BattleQuest-TTZ.git
```

> Também é possível clonar com a opção "Clonar Repositório" ao abrir o Visual Studio.

### Passo 2: Abrir o Projeto no Visual Studio

1. Abra o **Visual Studio 2026 ou 2022**
2. Clique em **"Abrir um projeto ou uma solução"**
3. Navegue até a **pasta raiz** `BattleQuest-TTZ/` e selecione o arquivo `BattleQuest.slnx`
4. Aguarde o Visual Studio restaurar os pacotes de dependência do NuGet automaticamente

#### Abrir o Terminal Integrado

No Visual Studio, abra o terminal integrado que será usado para todos os comandos:

- Vá em **Exibir → Terminal** (ou pressione `Ctrl + '`)
- O terminal abrirá na parte inferior da janela, já posicionado na raiz do projeto

#### Visualizar o Gerenciador de Soluções

Se o Gerenciador de Soluções não estiver visível:

- Vá em **Exibir → Gerenciador de Soluções** (ou pressione `Ctrl + Alt + L`)

### Passo 3: Configurar o Banco de Dados

#### 3.1 Iniciar o Docker Desktop

1. Abra o aplicativo **Docker Desktop**
2. Se for o primeiro acesso no Windows, pode ser solicitado atualizar o WSL
3. Abra o terminal integrado do Visual Studio e execute:

```bash
wsl --update
```

4. Aguarde o Docker Desktop inicializar completamente.

#### 3.2 Criar e Iniciar o Container PostgreSQL

No **terminal integrado do Visual Studio** (certifique-se de estar na raiz do projeto), execute:

```bash
docker compose up -d
```

> **📝 O que este comando faz:** Cria e inicia o container PostgreSQL em segundo plano (parâmetro `-d`).

**Verificar se o container está rodando:**

No terminal, execute:

```bash
docker ps
```

Você deve ver algo como:

```
CONTAINER ID   IMAGE         COMMAND                  STATUS         PORTS
abc123def456   postgres:16   "docker-entrypoint.s…"   Up 10 seconds  0.0.0.0:5433->5432/tcp
```

### Passo 4: Aplicar Migrações do Banco de Dados

Com o container PostgreSQL em execução, crie o banco de dados e as tabelas.

No **terminal integrado do Visual Studio**, execute os seguintes comandos:

**1. Instalar o Entity Framework CLI:**

```bash
dotnet tool restore
```

**2. Criar o banco de dados e aplicar as migrações:**

```bash
dotnet ef database update --project src/BattleQuest.Infrastructure --startup-project src/BattleQuest.API --context BattleQuestDbContext
```

> Aguarde o comando finalizar sua execução.

**O que cada comando faz:**
- `dotnet tool restore` → Instala o Entity Framework CLI
- `dotnet ef database update` → Aplica as migrações e cria o schema

**Resultado esperado:**

```
Build started...
Build succeeded.
...
Done.
```

---

## 📥 Importação de Logs

O projeto inclui uma aplicação console dedicada para importar arquivos de log em lote para o banco de dados.

O importador executa a importação em **lotes de 5.000 registros** por vez, o que melhora significativamente a performance ao reduzir operações de I/O no banco de dados, otimizar transações e manter o uso de memória constante. Além disso, utiliza **cache em memória** para entidades de referência (Players, Zones, Items, Bosses, Quests, Channels, ActionTypes), evitando consultas repetidas ao banco e acelerando o processamento.

### Arquivos de Log Disponíveis

Na pasta `src/BattleQuest.Importer/Data/`, você encontrará dois arquivos:

| Arquivo                 | Eventos  | Descrição                                    |
|-------------------------|----------|----------------------------------------------|
| `game_log_small.txt`    | 6.000    | Arquivo com os logs dos primeiros eventos |
| `game_log_large.txt`    | 126.017  | Arquivo completo com todos os eventos        |

> **⚠️ Importante:** Os eventos do `game_log_small.txt` são os 6.000 primeiros do `game_log_large.txt`. Se importar o pequeno primeiro e depois o grande, o sistema detectará 6.000 duplicados e não os importará novamente.

### Como Importar

#### Importar o Arquivo Pequeno (Teste Rápido)

No **terminal integrado do Visual Studio** (certifique-se de estar na raiz do projeto), execute:

```bash
dotnet run --project src/BattleQuest.Importer -- "Data/game_log_small.txt"
```

#### Importar o Arquivo Completo

No **terminal integrado do Visual Studio**, execute:

```bash
dotnet run --project src/BattleQuest.Importer -- "Data/game_log_large.txt"
```

### Importar Arquivo Customizado

Você pode processar qualquer arquivo de log no formato esperado.

**Opção 1: Adicionar na pasta Data**

1. Coloque seu arquivo em `src/BattleQuest.Importer/Data/`
2. No **terminal integrado do Visual Studio**, execute:

```bash
dotnet run --project src/BattleQuest.Importer -- "Data/seu_arquivo.txt"
```

**Opção 2: Usar caminho absoluto**

No **terminal integrado do Visual Studio**, execute:

```bash
dotnet run --project src/BattleQuest.Importer -- "C:/logs/meu_log.txt"
```

### Formato do Arquivo de Log Esperado (exemplos)

```
2025-01-15 14:32:10 [COMBAT] BOSS_DAMAGE player_id=p1 boss_name=LichQueen damage=293
2025-01-15 14:32:15 [GAME] ZONE_ENTER player_id=p2 zone=MysticLake
2025-01-15 14:32:20 [SYSTEM] SERVER_ANNOUNCEMENT text="Double XP event started"
2025-01-15 14:32:25 [COMBAT] DEATH victim_id=p5 killer_id=p1 method=sword
2025-01-15 14:32:30 [CHAT] MESSAGE player_id=p5 message="Anyone need help?"
...
```

### Validações de Arquivo

O importador valida automaticamente:

✅ **Tipos de arquivo aceitos:** `.txt`, `.log`, `.csv`, `.tsv` (ou sem extensão)  
❌ **Rejeitados:** `.xlsx`, `.docx`, `.pdf`, `.jpg`, `.png`, etc.

✅ **Detecta arquivos binários** e exibe erro amigável  
✅ **Detecta diretórios** quando um caminho de pasta é passado  
✅ **Valida existência** do arquivo no sistema

### Exemplo de Saída da Importação

```
====================================================
  Ferramenta de Importação de Logs BattleQuest
====================================================

Validando o caminho do arquivo...
Arquivo validado com sucesso.

Iniciando importação de: game_log_large.txt

[Progresso] Linha 5000: Processando... (3.2s decorridos)
[Progresso] Linha 10000: Processando... (6.5s decorridos)
...

====================================================
  Resumo da Importação
====================================================

Total de Linhas Processadas:   126017
Eventos Importados:             125890
Ignorados (Duplicatas):         127
Erros:                          0
Duração:                        42.3 segundos

Importação concluída com sucesso!
====================================================
```

---

## ▶️ Executando o Projeto

### 1. Recompilar a Solução

No **Gerenciador de Soluções**:

1. Clique com o botão direito em **`Solução 'BattleQuest'`** (primeiro item da árvore)
2. Selecione **"Recompilar Solução"**
3. Aguarde a compilação finalizar (veja o progresso na barra inferior)

### 2. Executar a API

Na **barra superior** do Visual Studio, você verá dois botões verdes:

- **▶️ Preenchido** → Execução com debugger (modo de depuração)
- **▷ Bordas verdes** → Execução sem debugger (mais rápido)

**Clique no botão ▷ (bordas verdes)** para iniciar a aplicação.

### 3. Certificado SSL (Apenas na Primeira Execução)

Se for a primeira vez executando o projeto:

1. Um aviso aparecerá pedindo para **confiar no certificado SSL do ASP.NET**
2. Clique em **"Sim"**
3. Um segundo aviso de segurança aparecerá
4. Clique em **"Sim"** novamente

> **📝 Nota:** Se esses avisos não aparecerem, o certificado já está instalado.

### 4. Acessar o Swagger

O navegador abrirá automaticamente em:

🔗 **https://localhost:7258/swagger/index.html**

Você verá a interface interativa do Swagger UI com todos os endpoints documentados e **já poderá testar com os dados importados anteriormente**.

### 5. Autenticar no Swagger

Para testar os endpoints, é necessário autenticar:

1. No Swagger, clique no botão **"Authorize"** (canto superior direito com ícone de cadeado)
2. No campo que aparecer, digite:

```
NANDATE-dev-token
```

3. Clique em **"Authorize"** e depois em **"Close"**
4. Agora você pode testar todos os endpoints com os dados reais!

---

## 🧪 Testes

O projeto possui **144 testes automatizados** divididos em três categorias, totalizando cobertura completa do sistema.

### Executar Todos os Testes

**1. Localizar os Projetos de Teste**

No **Gerenciador de Soluções**, expanda a pasta **tests**. Você verá:
- `BattleQuest.API.Tests`
- `BattleQuest.Application.Tests`
- `BattleQuest.Integration.Tests`

**2. Executar os Testes**

Clique com o botão direito em qualquer projeto de teste (ou na solução inteira) e selecione **"Executar Testes"**.

> **💡 Dica:** Ao executar pela solução, todos os 144 testes serão executados de uma vez.

**3. Visualizar Resultados**

O **Gerenciador de Testes** será aberto automaticamente com os resultados:

```
Total de testes: 144
Aprovado: 144 ✅
Falhou: 0
Ignorado: 0
```

### Executar via Terminal (Alternativa)

Se preferir usar o terminal integrado do Visual Studio, execute:

```bash
dotnet test
```

Para executar apenas um projeto específico:

```bash
dotnet test tests/BattleQuest.API.Tests/BattleQuest.API.Tests.csproj
dotnet test tests/BattleQuest.Application.Tests/BattleQuest.Application.Tests.csproj
dotnet test tests/BattleQuest.Integration.Tests/BattleQuest.Integration.Tests.csproj
```

### Estrutura dos Testes

| Projeto                              | Testes | Descrição                                |
|--------------------------------------|--------|------------------------------------------|
| `BattleQuest.API.Tests`              | 32     | Testes dos controllers e autenticação    |
| `BattleQuest.Application.Tests`      | 78     | Testes de parsing, mapping e pipeline    |
| `BattleQuest.Integration.Tests`      | 34     | Testes de integração com banco real      |

### Cobertura de Testes

Os testes cobrem:

- ✅ **Controllers:** Validação de respostas HTTP, códigos de status, autenticação
- ✅ **Parsing:** Extração correta de dados dos logs
- ✅ **Mapping:** Conversão de dados parseados para entidades
- ✅ **Queries:** Consultas ao banco de dados e agregações
- ✅ **Import Pipeline:** Processo completo de importação
- ✅ **Validações:** Tratamento de erros e casos extremos

---

## 📚 Documentação da API

### Swagger / OpenAPI

#### Acessar a Documentação Interativa

Com a API em execução, acesse:

🔗 **https://localhost:7258/swagger/index.html**

O Swagger UI oferece:
- 📖 Documentação completa de todos os endpoints
- 🧪 Interface para testar requisições diretamente no navegador
- 📋 Schemas dos modelos de dados (DTOs)
- 🔐 Autenticação via header `X-API-TOKEN`

#### Como Usar o Swagger

1. **Autenticar:**
   - Clique no botão **"Authorize"** no canto superior direito
   - Digite o token: `NANDATE-dev-token`
   - Clique em **"Authorize"** e depois **"Close"**

2. **Testar um Endpoint:**
   - Expanda o endpoint desejado (ex: `GET /api/players`)
   - Clique em **"Try it out"**
   - Preencha os parâmetros (se necessário)
   - Clique em **"Execute"**
   - Veja a resposta com status code, body e headers

### Postman Collection

#### Download da Coleção

A coleção do Postman está localizada em:

📁 `docs/postman/BattleQuest_APIv1.postman_collection.json`

#### Importar no Postman

> **⚠️ Importante:** Para testar os endpoints no Postman, a **API precisa estar em execução**. Se ainda não iniciou a aplicação, consulte a seção [▶️ Executando o Projeto](#️-executando-o-projeto).

1. **Abrir o Postman**
   - Se não tiver instalado, baixe em: [https://www.postman.com/downloads/](https://www.postman.com/downloads/)

2. **Importar a Coleção:**
   - Realize seu login ou crie sua conta
   - Quando estiver logado, clique no botão **"Import"** no canto superior esquerdo
   - Arraste o arquivo `BattleQuest_APIv1.postman_collection.json` para a janela
   - **OU** clique em **"Choose Files"** e selecione o arquivo
   - Clique em **"Import"**

3. **Testar os Endpoints:**
   - Expanda a coleção `BattleQuest API v1`
   - Selecione um endpoint (ex: `GET Players - List All`)
   - Clique em **"Send"**
   - Veja a resposta na parte inferior

#### Estrutura da Coleção Postman

A coleção contém **6 pastas** organizadas por funcionalidade:

| Pasta         | Endpoints | Descrição                                    |
|---------------|-----------|----------------------------------------------|
| **Dashboard** | 1         | Métricas consolidadas e estatísticas gerais  |
| **Events**    | 1         | Consulta de histórico de eventos             |
| **Items**     | 1         | Estatísticas de itens coletados              |
| **Leaderboard** | 1       | Ranking de jogadores por pontuação           |
| **Players**   | 2         | Lista de jogadores e estatísticas individuais |

---

## 🎯 Recursos e Funcionalidades

### Endpoints da API

#### 1️⃣ Players
- `GET /api/players` → Lista todos os jogadores com dados básicos
- `GET /api/players/{id}/stats` → Estatísticas detalhadas de um jogador

**Exemplo de resposta:**
```json
{
  "playerId": "p1",
  "name": "DragonSlayer",
  "lastKnownLevel": 23
  "totalScore": 15420,
  "totalDeaths": 8,
  "totalKills": 42,
  "itemsCollected": 127,
  "questsCompleted": 15,
  "totalXpEarned": 8500,
  "totalGoldEarned": 3200
}
```

#### 2️⃣ Leaderboard
- `GET /api/leaderboard?limit=50` → Ranking de jogadores por pontuação

**Exemplo de resposta:**
```json
[
  {
    "rank": 1,
    "playerId": "p1",
    "name": "Eve",
    "totalScore": 15420,
    "lastKnownLevel": 35
  },
  {
    "rank": 2,
    "playerId": "p3",
    "playerName": "Diana",
    "totalScore": 14830,
    "lastKnownLevel": 32
  }
]
```

#### 3️⃣ Events
- `GET /api/events?limit=50&includeRaw=true` → Últimos eventos do jogo

**Exemplo de resposta:**
```json
[
  {
    "eventId": 6000,
    "occurredAt": "2025-08-31T11:39:38+00:00",
    "channel": "CHAT",
    "actionType": "MESSAGE",
    "playerId": "p5",
    "victimPlayerId": null,
    "killerPlayerId": null,
    "questId": null,
    "zone": null,
    "item": null,
    "boss": null,
    "quantity": null,
    "xp": null,
    "gold": null,
    "hp": null,
    "damage": null,
    "method": null,
    "playerLevel": null,
    "points": null,
    "reason": null,
    "locationX": null,
    "locationY": null,
    "messageText": "Going to the cave",
    "raw": "2025-08-31 11:39:38 [CHAT] MESSAGE player_id=p5 message=\"Going to the cave\""
  }
]
```

#### 4️⃣ Items
- `GET /api/items/top?limit=50` → Itens mais coletados

**Exemplo de resposta:**
```json
[
  {
    "itemName": "HEALTH_POTION",
    "totalCollected": 2378,
    "collectionCount": 434
  },
  {
    "itemName": "MANA_POTION",
    "totalCollected": 2193,
    "collectionCount": 395
  }
]
```

#### 5️⃣ Dashboard (Opcional - Extra)
- `GET /api/dashboard?startDate=2026-01-01&endDate=2026-01-31` → Métricas consolidadas

**Exemplo de resposta:**
```json
{
  "totalActivePlayers": 6,
  "totalScoreAccumulated": 1190492,
  "topCollectedItems": [
    {
      "itemName": "HEALTH_POTION",
      "totalCollected": 2378,
      "collectionCount": 434
    },
    {
      "itemName": "MANA_POTION",
      "totalCollected": 2193,
      "collectionCount": 395
    },
  ],
  "topPlayerDeaths": [
    {
      "playerId": "p3",
      "name": "Charlie",
      "totalDeaths": 687
    },
    {
      "playerId": "p6",
      "name": "Frank",
      "totalDeaths": 685
    }
  ],
  "bossesDefeated": [
    {
      "bossName": "SHADOWDRAGON",
      "defeatCount": 1343
    },
    {
      "bossName": "GOLEMKING",
      "defeatCount": 1302
    }
  ],
  ...
}
```

### Autenticação

Todos os endpoints requerem autenticação via header:

```
X-API-TOKEN: NANDATE-dev-token
```

**Códigos de resposta relacionados à autenticação:**
- `200 OK` → Requisição bem-sucedida
- `401 Unauthorized` → Token ausente ou inválido
- `503 Service Unavailable` → Banco de dados indisponível

### Tratamento de Erros

A API retorna respostas padronizadas no formato **ProblemDetails** (RFC 7807):

**Exemplo - Banco indisponível (503):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.4",
  "title": "Serviço de Banco de Dados Indisponível",
  "status": 503,
  "detail": "Não foi possível conectar ao banco de dados. O serviço pode estar temporariamente indisponível.",
  "possibleCauses": [
    "O container Docker do PostgreSQL não está em execução",
    "O banco de dados está reiniciando",
    "Problemas de rede ou firewall",
    "Configuração de conexão incorreta"
  ],
  "suggestion": "Verifique se o Docker está em execução: docker ps | findstr postgres"
}
```

### Diagrama do Banco de Dados

Para visualizar o modelo de dados completo, abra o diagrama:

📁 `docs/database/BattleQuestDiagram.drawio`

Você pode abrir com [draw.io](https://app.diagrams.net/) ou importar no Visual Studio Code com a extensão Draw.io Integration.

Você também pode visualizar o diagrama por meio da imagem png disponibilizada no mesmo diretório:

📁 `docs/database/battlequest_diagram.png`

**Entidades principais:**
- `players` → Jogadores
- `events` → Eventos
- `items` → Itens
- `bosses` → Chefes
- `quests` → Missões
- `zones` → Zonas
- `channels` → Canais de jogo
- `actiontype` → Tipos de mensagem de ação

---

## 🔍 Troubleshooting

### Problema: Container PostgreSQL não inicia

**Erro:**
```
Error: port 5433 is already in use
```

**Solução:**
```bash
# Verificar processos usando a porta
netstat -ano | findstr :5433

# Parar o container existente
docker ps
docker stop <container_id>

# Ou alterar a porta no docker-compose.yml
ports:
  - "5434:5432"  # Usar porta diferente
```

### Problema: Erro ao aplicar migrações

**Erro:**
```
Npgsql.NpgsqlException: Connection refused
```

**Solução:**
1. Verifique se o Docker está em execução: `docker ps`
2. Verifique os logs do container: `docker logs <container_id>`
3. Reinicie o container: `docker restart <container_id>`

### Problema: Erro 401 no Swagger

**Solução:**
1. Clique no botão **"Authorize"**
2. Digite: `NANDATE-dev-token`
3. Clique em **"Authorize"** e depois **"Close"**

### Problema: Certificado SSL não confiável

**Solução:**
```bash
# Instalar certificado de desenvolvimento
dotnet dev-certs https --trust
```

---

## 📄 Licença

Este projeto está licenciado sob a [MIT License](LICENSE).

---

## 🤝 Contribuições

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests.

---

**Desenvolvido com ❤️ usando .NET 8.0 e Clean Architecture**