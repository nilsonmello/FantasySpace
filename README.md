<h1 align="center">Fantasia Espacial</h1>

---

## Sobre

O projeto se baseia em um ciclo procedural de ambiente semi inorgânico unido a necessidade de realizar tarefas e atividades para escapar enquanto foge e arquiteta métodos de distrair a criatura que o persegue.

---

# Estrutura de Diretórios

> [!TIP]
> Esta estrutura serve como um guia e pode ser alterada conforme a necessidade do projeto.
> Sempre que houver mudanças significativas, atualize este documento.

```text
Assets/
├── Animations/            # Animações e Animation Controllers
├── Art/                   # Recursos visuais
│   ├── Backgrounds/       # Cenários e fundos
│   ├── Effects/           # Partículas e efeitos visuais
│   ├── Sprites/           # Sprites organizados por categoria
│   ├── Tilesets/          # Tiles e Tilemaps
│   └── UI/                # Elementos visuais da interface
├── Audio/
│   ├── Music/             # Trilhas sonoras
│   └── SFX/               # Efeitos sonoros
├── Fonts/                 # Fontes utilizadas
├── Materials/             # Materiais
├── Prefabs/               # Prefabs reutilizáveis
├── Resources/             # Assets carregados em tempo de execução
├── Scenes/                # Cenas do projeto
├── ScriptableObjects/     # Configurações e bancos de dados
├── Scripts/
│   ├── AI/                # Inteligência artificial
│   ├── Core/              # Sistemas principais
│   ├── Gameplay/          # Mecânicas do jogo
│   ├── Input/             # Sistema de entrada
│   ├── Managers/          # Game Managers
│   ├── UI/                # Interface
│   ├── Utilities/         # Classes auxiliares
│   └── Weapons/           # Sistema de armas (caso exista)
├── Settings/              # Configurações do projeto
├── StreamingAssets/       # Arquivos acessados diretamente
└── TextMesh Pro/          # Assets do TextMesh Pro
```

---

# 📂 Descrição das Pastas

| Pasta | Descrição |
|--------|-----------|
| **Animations** | Animações e Animation Controllers. |
| **Art** | Recursos gráficos utilizados pelo jogo. |
| **Audio** | Músicas e efeitos sonoros. |
| **Fonts** | Fontes utilizadas na interface. |
| **Materials** | Materiais utilizados pelos sprites e efeitos. |
| **Prefabs** | Objetos reutilizáveis do projeto. |
| **Resources** | Assets carregados via `Resources.Load()`. |
| **Scenes** | Todas as cenas do projeto. |
| **ScriptableObjects** | Dados configuráveis do jogo. |
| **Scripts** | Toda a lógica do projeto organizada por responsabilidade. |
| **Settings** | Configurações do Unity. |
| **StreamingAssets** | Arquivos acessados diretamente em tempo de execução. |
| **TextMesh Pro** | Recursos do TextMesh Pro. |

---

# Convenções de Código

É recomendado seguir as convenções oficiais da Microsoft para C#.

https://learn.microsoft.com/pt-br/dotnet/csharp/fundamentals/coding-style/coding-conventions

---

# ✅ Convenção de Commits

> [!NOTE]
> Utilize o padrão:

```text
type(scope): description
```

Exemplos:

```text
feat(player): add charged attack
feat(ui): create pause menu
fix(enemy): fix boss collision
refactor(audio): reorganize AudioManager
docs(readme): update project structure
style(ui): adjust menu spacing
```

Os tipos seguem a convenção definida pelo **Conventional Commits 1.0.0**:

https://www.conventionalcommits.org/pt-br/v1.0.0/

Tipos recomendados:

- feat
- fix
- docs
- style
- refactor
- perf
- test
- chore

Baseado em:

https://www.conventionalcommits.org/pt-br/v1.0.0/

---
