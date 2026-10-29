# Prisma Dash 🔷🔶

Corredor infinito 3D para **WebGL**, feito 100% em código para **Unity 2022.3 LTS** (Built-in Render Pipeline). O quadro de referência é **retrato ~9:16** (1080×1920). No celular o canvas ocupa a tela; no navegador de desktop a página mantém um quadro retrato centralizado, com faixas escuras nas laterais.

Jogue em **https://gugamazzotti.github.io/prisma-dash/** (depois de configurar os secrets e o GitHub Pages, abaixo).

## Conceito
Sua esfera tem **polaridade**: **Ciano** ou **Magenta**. Portais coloridos ocupam a pista inteira e só deixam passar quem está com a **mesma cor** — não dá para desviar, só *virar* a cor certa. Ao mesmo tempo, paredes escuras bloqueiam pistas e precisam ser desviadas, e orbes da sua cor alimentam o **combo** (orbes da cor errada quebram o combo). Na dificuldade alta, portais e paredes chegam juntos: decisão dupla em frações de segundo.

- Velocidade sobe de 14 → 42 u/s em ~100 s; o espaçamento entre obstáculos diminui.
- Pontos por distância + bônus (portal 10×combo, orbe 5×combo). Recorde salvo em `PlayerPrefs` (persistente no navegador).
- Juice: camera shake por "trauma", partículas, áudio sintetizado em runtime, câmera lenta no game over.

## Controles
O jogo é de celular em retrato. O texto na tela só explica o toque:

| Ação | Toque |
|---|---|
| Mudar de pista | Tocar o terço esquerdo ou direito, ou deslizar para os lados |
| Trocar cor | Tocar o centro, ou deslizar para cima ou para baixo |
| Começar/Reiniciar | Tocar |

O teclado continua funcionando, sem aparecer no tutorial: `A`/`D` ou `←`/`→` mudam de pista; `Espaço`, `W`, `↑` ou o clique direito trocam a cor; `Espaço` ou `Enter` começam ou reiniciam. O clique esquerdo usa as mesmas faixas do toque. O gesto que inicia a partida não muda de pista nem de cor.

## Como abrir
1. Unity Hub → **Add project from disk** → selecione esta pasta (use um Unity **2022.3.x**; o projeto está em **2022.3.20f1**. Se a versão for diferente, aceite a atualização).
2. Abra `Assets/Scenes/Main.unity` e dê **Play**.
   Na verdade qualquer cena funciona: `Bootstrap.cs` usa `[RuntimeInitializeOnLoadMethod]` e monta câmera, luz, pista, jogador e UI sozinho.
3. No Game view, escolha uma resolução alta, por exemplo **1080×1920**. Qualquer proporção funciona: a câmera alarga o FOV quando a tela é mais estreita que 9:16, para as três pistas continuarem no quadro.

## Build WebGL
- Instale o módulo **WebGL Build Support** no Unity Hub.
- Menu **Prisma Dash → Build WebGL** (Gzip com *decompression fallback*, template retrato e canvas padrão **540×960**; gera em `Build/WebGL`).
- Linha de comando:
  ```
  Unity -batchmode -quit -projectPath . -executeMethod PrismaDash.EditorTools.WebGLBuilder.Build -buildTarget WebGL
  ```
- Teste local: `npx http-server Build/WebGL` (não abra o `index.html` direto pelo `file://`).

O template `Assets/WebGLTemplates/PrismaPortrait` desenha o canvas em 9:16. Se a janela for mais larga que alta (desktop), o jogo fica num quadro retrato no centro. Se a janela já for retrato (celular), o canvas ocupa a tela inteira.

## Publicar
- **itch.io**: compacte o *conteúdo* de `Build/WebGL` (index.html na raiz do zip), crie um projeto "HTML", envie o zip e marque "This file will be played in the browser". Tamanho sugerido **540×960**, ative fullscreen e "mobile friendly".
- **GitHub Pages**: o workflow em `.github/workflows/webgl.yml` publica sozinho. Veja a seção abaixo.

## GitHub Actions e GitHub Pages

Cada push na branch `main` (ou um disparo manual do workflow **WebGL**) compila o projeto com [GameCI](https://game.ci/docs/github/builder) (`game-ci/unity-builder@v4`, Unity lida em `ProjectSettings/ProjectVersion.txt`) e publica o build com `actions/deploy-pages`. A pasta `Library` entra no cache do Actions para os builds seguintes.

A compressão do player é **Gzip com decompression fallback**. O GitHub Pages não envia o header `Content-Encoding`; o fallback faz o loader descomprimir os arquivos no navegador. Sem isso, o jogo não inicia nesse host.

### Os três secrets

No repositório: **Settings → Secrets and variables → Actions → New repository secret**.

| Secret | O que colar |
|---|---|
| `UNITY_LICENSE` | o conteúdo inteiro do arquivo `.ulf` |
| `UNITY_EMAIL` | o e-mail da conta Unity usada na licença |
| `UNITY_PASSWORD` | a senha dessa conta |

O GameCI não guarda esses dados: eles só existem no job, para ativar o editor. A [documentação de ativação](https://game.ci/docs/github/activation) recomenda uma senha só com letras e números (sem caracteres especiais).

#### Licença Personal (arquivo `.ulf`)

Siga o fluxo de licença **Personal** do GameCI — não use o fluxo de serial Pro/Plus:

1. Instale o Unity Hub e entre com a conta que o CI vai usar.
2. Abra **Unity Hub → Preferences (ou Settings) → Licenses → Add** e escolha **Get a free personal license**. Mesmo que já exista uma licença na lista, use **Add**: é esse passo que gera o arquivo `.ulf`.
3. Ache o arquivo (a licença não fica presa a um sistema nem a uma versão do editor; pode ativar no Windows e usar o mesmo arquivo no runner Ubuntu):
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
4. Abra o `.ulf` num editor de texto e cole **todo** o conteúdo no secret `UNITY_LICENSE` (o XML completo, não o caminho do arquivo).
5. Crie também `UNITY_EMAIL` e `UNITY_PASSWORD` com as credenciais dessa mesma conta.

Se o Hub não mostrar a opção que gera o `.ulf`, o GameCI documenta alternativas na mesma página e no Discord do projeto.

### Ativar o Pages

1. **Settings → Pages → Build and deployment → Source: GitHub Actions**.
2. Confirme que os três secrets existem.
3. Faça push na `main`, ou em **Actions → WebGL → Run workflow**.
4. Quando o job **Deploy GitHub Pages** ficar verde, o jogo abre em:

https://gugamazzotti.github.io/prisma-dash/

O primeiro build baixa a imagem do editor e pode levar bastante tempo. Os seguintes reaproveitam o cache de `Library`.

## Arquivos
```
Assets/
  Scenes/Main.unity              cena mínima (só um GameObject marcador; fog linear ligado)
  Resources/Materials/Lit.mat    Standard com _EMISSION, carregado em runtime
  Resources/Materials/Unlit.mat  Sprites/Default para as partículas
  Shaders/PrismaVariants.shadervariants
                                 variantes de luz direcional, fog e emissão
  WebGLTemplates/PrismaPortrait  página do player: quadro retrato e letterbox no desktop
  Scripts/Runtime/
    Bootstrap.cs                 entrada automática via RuntimeInitializeOnLoadMethod
    GameManager.cs               estados (menu/jogo/fim), input, spawn, dificuldade, colisão por pista
    Palette.cs                   cores; clona os materiais de Resources (sem Shader.Find)
    CameraRig.cs                 câmera de perseguição; FOV acompanha a proporção
    Fx.cs                        ParticleSystem único com Emit manual
    ProceduralAudio.cs           efeitos sonoros sintetizados (AudioClip.Create)
    Hud.cs                       Canvas em 1080×1920 + Text legado criados em código
  Scripts/Editor/WebGLBuilder.cs menu de build WebGL (também é o buildMethod do Actions)
Packages/manifest.json           apenas uGUI + módulos necessários
ProjectSettings/                 Unity 2022.3.20f1, canvas WebGL 540×960, Gzip + fallback
.github/workflows/webgl.yml      build WebGL no push da main e deploy no GitHub Pages
```

## Notas técnicas
- Materiais: a cena não tem renderers, então o build WebGL removia o shader Standard. Em runtime o material padrão do `CreatePrimitive` caía no shader de erro e a pista inteira ficava magenta. Agora `Lit.mat` e `Unlit.mat` ficam em `Resources`, o Standard (fileID 46) e o `Sprites/Default` estão em Always Included Shaders, e uma coleção de variantes pré-carrega luz direcional, fog linear e `_EMISSION`. O código clona esses assets; não usa `Shader.Find`.
- Cores: jogador e portais ciano `(0.10, 0.95, 1)` ou magenta `(1, 0.15, 0.70)` com emissão; paredes escuras sem emissão; orbes da mesma cor com emissão mais forte; partículas usam o material unlit branco e a cor do vértice.
- Sem TextMeshPro (evita o import de TMP Essentials); fonte `LegacyRuntime.ttf` embutida (a fonte embutida do Unity 2022.2+).
- Colisão é lógica (pista + cruzamento de z), sem física — determinística e barata no WebGL.
- No WebGL o áudio só inicia após a primeira interação do usuário (regra dos navegadores) — o clique de início resolve isso.
- A câmera fica mais alta e mais atrás do que num enquadramento de paisagem, e o FOV vertical é calculado para a largura dos arcos caber em 9:16. Em telas ainda mais altas o FOV abre; em telas mais largas o FOV de retrato permanece, então o jogador não sai por baixo.
- A licença Unity Personal exige a splash screen da Unity. Ela permanece ligada.
