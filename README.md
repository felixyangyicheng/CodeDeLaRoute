# Code de la Route — Application d'entraînement

**🇫🇷 Français** | [🇬🇧 English](#english) | [🇨🇳 中文](#chinese)

---

Application web progressive (PWA) d'entraînement au Code de la Route français, couvrant les épreuves théoriques Auto (ETG) et Moto (ETM). Questions à choix unique et multiple, mode examen chronométré, suivi de progression et support hors-ligne.

## Fonctionnalités

- **🎮 Mode Quiz** — Quiz aléatoires de 5 à 80 questions, filtrables par catégorie et type de véhicule
- **⏱️ Mode Examen** — Simulation fidèle de l'ETG officiel : 40 questions, 30 minutes, 20s par question avec barre de progression, auto-avance si temps écoulé, seuil ADMIS/NON ADMIS à 35/40 (5 fautes max). Questions réparties selon les 10 thèmes officiels
- **📚 Mode Termes** — Exercices ciblés par termes du Code (Vitesse, Priorité, Dépassement, Alcool, etc.). Extraction automatique de 30 termes-clés avec compteur de questions disponibles
- **📖 Mode Étude** — Parcours libre des questions avec réponses et explications toujours visibles. Filtres par catégorie, difficulté et véhicule
- **🏍️ Double véhicule** — Questions distinctes pour le permis Auto (ETG) et Moto (ETM), avec sélecteur intégré
- **🏷️ 10 thèmes ETG officiels** — Mapping automatique des 28 catégories vers les 10 thèmes de l'examen réel (L, C, R, M, D, P, N, E, S, U)
- **🔀 Variantes intelligentes** — Chaque question est déclinée en jusqu'à 5 variantes via 5 stratégies algorithmiques (permutation d'options, négation, substitution de distracteurs, inversion de rôle, perturbation numérique). Le pool effectif dépasse 1400 questions
- **🖼️ Images contextuelles** — Panneaux de signalisation officiels (Wikimedia Commons, domaine public) et photos de situations réelles (Unsplash)
- **📊 Statistiques** — Historique détaillé, score moyen, progression par catégorie, meilleur score, temps total. Stockage local (localStorage)
- **📱 PWA** — Installable sur mobile/desktop, fonctionnement hors-ligne, manifeste, service worker avec cache du shell applicatif et des données

## Stack technique

| Composant | Technologie |
|-----------|-------------|
| Framework | .NET 11 Blazor WebAssembly (CoreCLR) |
| UI | MudBlazor 9.4 + QuickGrid |
| Dev Server | Gateway (ex-DevServer) |
| Données | JSON statique chargé via HttpClient |
| Stockage | localStorage (statistiques) |
| Cache | Service Worker (PWA) |
| Images | Wikimedia Commons + Unsplash |

## Lancement

```bash
cd CodeDeLaRoute
dotnet run
```

L'application s'ouvre sur `https://localhost:5001`.

## Structure du projet

```
CodeDeLaRoute/
├── Models/
│   └── Question.cs              # Modèles : Question, QuizResult, EtgThemes, TermesGlossaire
├── Services/
│   ├── QuestionService.cs        # Chargement JSON, cache, quiz par catégorie/terme/thème
│   ├── QuestionVariantService.cs # 5 stratégies de génération de variantes
│   └── StatisticsService.cs      # Persistance et calcul des statistiques
├── Pages/
│   ├── Home.razor                # Accueil avec sélecteur Auto/Moto, catégories, Examen Blanc
│   ├── Quiz.razor                # Quiz + mode examen ETG + quiz par termes
│   ├── Termes.razor              # Exercices par termes (30 termes-clés)
│   ├── Study.razor               # Mode étude avec filtres
│   └── Statistics.razor          # Tableau de bord statistique (QuickGrid)
├── Components/
│   └── StatCard.razor            # Carte de statistique réutilisable
├── Layout/
│   ├── MainLayout.razor          # Layout principal (AppBar + Drawer)
│   └── NavMenu.razor             # Menu de navigation
├── wwwroot/
│   ├── data/
│   │   ├── questions.json                    # 80 questions Auto
│   │   ├── questions_moto.json               # 60 questions Moto
│   │   ├── questions_auto_supplement.json    # +80 questions Auto
│   │   └── questions_moto_supplement.json    # +70 questions Moto
│   ├── favicon.svg               # Icône SVG (volant aux couleurs du thème)
│   ├── manifest.json             # Manifeste PWA
│   ├── service-worker.js         # Service Worker (cache offline)
│   └── index.html                # Page hôte avec enregistrement PWA
└── Program.cs                    # Configuration DI et démarrage
```

## Banque de questions

| Type | Base | Supplément | Total | Avec variantes (×5) |
|------|------|------------|-------|---------------------|
| Auto | 80 | 80 | **160** | ~800 |
| Moto | 60 | 70 | **130** | ~650 |
| **Total** | **140** | **150** | **290** | **~1450** |

### Catégories couvertes

**Auto :** Signalisation, Priorité, Vitesse, Alcool et Stupéfiants, Stationnement, Éclairage, Sécurité, Croisement et Dépassement, Environnement, Piétons et Cyclistes, Premiers Secours, Permis à Points, Conditions Météo, Tunnels et Autoroutes, Distractions et Fatigue, Documents et Assurance, Signalisation temporaire, Passages à niveau.

**Moto :** Équipement, Maîtrise, Conduite, Sécurité, Mécanique, Permis, Passager, Vitesse, Assurance et Documents, Chargement et Bagages, Conditions Météo Moto, Circulation Interfile.

---

## English

Progressive Web App (PWA) for French driving theory test practice, covering both Car (ETG) and Motorcycle (ETM) exams. Single and multiple-choice questions, timed exam mode, progress tracking, and offline support.

### Features

- **🎮 Quiz Mode** — Random quizzes from 5 to 80 questions, filterable by category and vehicle type
- **⏱️ Exam Mode** — Faithful ETG simulation: 40 questions, 30 minutes, 20s per question with progress bar, auto-advance on timeout, PASS/FAIL threshold at 35/40 (max 5 errors). Questions distributed across the 10 official themes
- **📚 Terms Mode** — Targeted practice by driving terms (Speed, Priority, Overtaking, Alcohol, etc.). Auto-extraction of 30 key terms with available question counts
- **📖 Study Mode** — Browse questions with answers and explanations always visible
- **🏍️ Dual Vehicle** — Separate question banks for Car (ETG) and Motorcycle (ETM)
- **🏷️ 10 official ETG themes** — Auto-mapping of 28 categories to the 10 real exam themes (L, C, R, M, D, P, N, E, S, U)
- **🔀 Smart Variants** — Up to 5 variants per question via algorithmic strategies (option shuffle, negation, distractor swap, role inversion, numeric perturbation). Effective pool exceeds 1400 questions
- **🖼️ Contextual Images** — Official road signs (Wikimedia Commons) and real-world photos (Unsplash)
- **📊 Statistics** — Detailed history, average score, per-category progress, stored in localStorage
- **📱 PWA** — Installable, offline-capable with service worker caching

### Tech Stack

.NET 11 Blazor WASM (CoreCLR) · MudBlazor 9.4 · QuickGrid · Gateway (dev server) · Static JSON via HttpClient · localStorage · Service Worker · Wikimedia Commons + Unsplash

### Run

```bash
cd CodeDeLaRoute
dotnet run
```

---

## 中文

法国驾照理论考试练习的渐进式 Web 应用（PWA），覆盖汽车（ETG）和摩托车（ETM）两项考试。支持单选和多选题、限时考试模式、学习进度追踪和离线使用。

### 功能特性

- **🎮 测验模式** — 随机生成 5 到 80 道题目，可按分类和车辆类型筛选
- **⏱️ 考试模式** — 忠实模拟官方 ETG：40 题、30 分钟、每题 20 秒倒计时带进度条、超时自动跳转、ADMIS/NON ADMIS 阈值 35/40（最多 5 错）。题目按 10 个官方主题分布
- **📚 术语模式** — 按驾驶术语定向练习（速度、优先权、超车、酒精等）。自动提取 30 个关键术语并显示可用题数
- **📖 学习模式** — 自由浏览题目，答案和解析始终可见
- **🏍️ 双车辆模式** — 汽车（ETG）和摩托车（ETM）各自独立的题库，内置切换器
- **🏷️ 10 个官方 ETG 主题** — 28 个分类自动映射至真实考试的 10 个主题（L、C、R、M、D、P、N、E、S、U）
- **🔀 智能变体** — 每道题通过 5 种算法策略（选项乱序、否定式改问、干扰项替换、角色互换、数值扰动）生成最多 5 道变体题，有效题库超过 1400 道
- **🖼️ 情景图片** — 官方路标（Wikimedia Commons 公共领域）和真实场景照片（Unsplash）
- **📊 数据统计** — 详细历史记录、平均分、分类进度、最佳成绩，存储于 localStorage
- **📱 PWA 支持** — 可安装到桌面/手机，离线可用，Service Worker 缓存

### 技术栈

.NET 11 Blazor WASM (CoreCLR) · MudBlazor 9.4 · QuickGrid · Gateway（开发服务器）· 静态 JSON（HttpClient 加载）· localStorage · Service Worker · Wikimedia Commons + Unsplash

### 运行

```bash
cd CodeDeLaRoute
dotnet run
```

### 项目结构

```
CodeDeLaRoute/
├── Models/Question.cs           # 数据模型：题目、测验结果、EtgThemes、TermesGlossaire
├── Services/
│   ├── QuestionService.cs        # JSON 加载、缓存、分类/术语/主题筛选测验
│   ├── QuestionVariantService.cs # 5 种变体生成策略
│   └── StatisticsService.cs      # 统计数据持久化与计算
├── Pages/                        # Blazor 页面组件（含 Termes.razor 术语练习）
├── Components/StatCard.razor     # 可复用统计卡片
├── Layout/                       # 布局与导航
├── wwwroot/data/                 # JSON 题库文件（4 个）
└── Program.cs                    # DI 注册与启动配置
```

### 题库规模

| 类型 | 基础题 | 补充题 | 合计 | 含变体 (×5) |
|------|--------|--------|------|-------------|
| 汽车 | 80 | 80 | **160** | ~800 |
| 摩托车 | 60 | 70 | **130** | ~650 |
| **总计** | **140** | **150** | **290** | **~1450** |

---
## 📝 Journal des modifications / Changelog / 改动日志

### v2.2 — Optimisations .NET 11 Preview 5 (2026-06-18)

- ⚡ **UseMonoRuntime=false** — Switches to CoreCLR WASM (replaces Mono): JIT/GC/async performance boost
- 🛡️ **CheckSdkVulnerabilities=true** — SDK vulnerability & EOL checks during build
- 🎯 **EqualityComparer\<T\>.Create** — QuestionVariantService: dedup O(n²) → O(1) via HashSet + key-selector
- 📊 **Single GroupBy** — StatisticsService: group categories once instead of twice
- 🎲 **Random.NextInteger\<T\>()** — 14 Random calls migrated to the new generic APIs
- 🧹 **using System.Net.Http.Json** — SDK auto-includes it; kept pending build verification

### v2.1 — Mode Exercices par Termes (2026-06-18)

- 🆕 **Page `/termes`** — Exercices ciblés par termes du Code de la Route
- 🆕 **30 termes-clés** extraits automatiquement : Vitesse, Priorité, Dépassement, Freinage/ABS, Alcool, Stupéfiants, Stationnement, Éclairage, Brouillard, Neige/Verglas, Pneus, Ceinture/Airbag, Angle mort, etc.
- 🆕 **Multi-sélection** de termes avec compteur de questions disponibles
- 🆕 **Exercices rapides** — 12 termes les plus fréquents en un clic (10 questions)
- 🆕 **Quiz par termes** — paramètre `termes` dans l'URL, bannière indicative dans le quiz
- 🆕 `TermesGlossaire` + `TermeDefinition` dans `Models/Question.cs`
- 🆕 `GetAvailableTermesAsync()`, `GetTermeQuizAsync()`, `GetMultiTermeQuizAsync()` dans `QuestionService`
- 🆕 Icônes pour les 9 nouvelles catégories (Signalisation temporaire, Circulation interfile, etc.)

### v2.0 — Alignement ETG Officiel + .NET 11 Preview 5 (2026-06-18)

- 🔄 **Mode Examen refondu** pour correspondre à l'ETG réel :
  - ⏱️ 30 minutes (au lieu de 40) · 🔢 40 questions fixé · ⏲️ 20s par question avec barre de progression
  - 🚀 Auto-avance si temps écoulé · ✅ Seuil ADMIS/NON ADMIS à 35/40
- 🏷️ **10 thèmes ETG officiels** — Mapping 28 catégories → thèmes L/C/R/M/D/P/N/E/S/U
- 📊 **Distribution thématique** — `GetExamQuizAsync()` respecte les quotas officiels (L=10, C=5, R=4, M=4, D=4, P=3, N=3, E=3, S=3, U=1)
- 🆕 **Bouton Examen Blanc** sur la page d'accueil
- 📦 **.NET 11 Preview 5** — Migration `DevServer` → `Gateway` + `QuickGrid` + `StaticWebAssetSpaFallbackEnabled`
- 🎯 **Quiz.razor** : `[SupplyParameterFromQuery]` remplace le parsing manuel des query strings
- 📊 **Statistics.razor** : `MudTable` → `QuickGrid` avec tri natif et pagination
- 🛠️ **gh-pages** : Correction structure Cloudflare Pages (aplatissement `wwwroot/`, placeholder boot résolu)

### v1.0 — Version initiale

- 🎮 Mode Quiz avec 290 questions de base (~1450 avec variantes)
- ⏱️ Mode Examen chronométré (40 min, 40 questions)
- 📖 Mode Étude avec filtres catégorie/difficulté
- 🏍️ Double véhicule Auto/Moto
- 🔀 Variantes intelligentes (5 stratégies algorithmiques)
- 📊 Statistiques avec localStorage
- 📱 PWA avec Service Worker
- 🌐 Trilingue FR/EN/CN

---

## Licence

MIT License — voir le fichier [LICENSE](LICENSE).
