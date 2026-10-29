using System.Collections.Generic;
using UnityEngine;

namespace PrismaDash
{
    /// <summary>
    /// PRISMA DASH — corredor infinito de polaridade.
    /// A nave tem uma cor (Ciano/Magenta). Portais coloridos só deixam passar quem tem a MESMA cor;
    /// paredes escuras precisam ser desviadas trocando de pista; orbes da sua cor dão combo.
    /// O mundo se move em direção ao jogador (que fica em z = 0); colisão é lógica por pista.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private enum State { Menu, Playing, GameOver }
        private enum Kind { Gate, Wall, Orb }

        private class Entity
        {
            public GameObject Go; public Kind Kind; public int Lane; public int Color; public bool Done;
        }

        private const float LaneWidth = 2.2f;
        private const float SpawnZ = 90f;
        private const float StartSpeed = 14f, MaxSpeed = 42f;
        // Desvio no último instante: já saiu da pista da parede, mas ainda está
        // a menos de 98% de uma pista de distância do centro dela.
        private const float GrazeReach = 0.98f;

        private State _state;
        private Transform _player;
        private Renderer _playerRenderer;
        private Material[] _playerMats;
        private CameraRig _cam;
        private Hud _hud;
        private Fx _fx;
        private ProceduralAudio _audio;
        private Light _light;

        private readonly List<Entity> _entities = new List<Entity>();
        private readonly List<Transform> _rings = new List<Transform>();
        private Material[] _gateMats, _orbMats;
        private Material _wallMat, _ringMat;

        private int _lane, _polarity, _combo, _best;
        private float _score, _speed, _time, _spawnTimer, _gameOverAt, _grazePulse;
        private Vector2 _touchStart;
        private bool _swallowTouch;

        // ---------------------------------------------------------------- setup
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Input.simulateMouseWithTouches = false; // evita input duplicado em celulares
            _best = PlayerPrefs.GetInt("prisma_best", 0);

            // Câmera e luz
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            _cam = camGo.AddComponent<CameraRig>();

            var lightGo = new GameObject("Light", typeof(Light));
            _light = lightGo.GetComponent<Light>();
            _light.type = LightType.Directional;
            _light.intensity = 1.1f;
            _light.shadows = LightShadows.None;
            _light.renderMode = LightRenderMode.ForcePixel;
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.3f, 0.45f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = Palette.Background;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 95f;

            // Materiais
            _gateMats = new[] { Palette.Lit(Palette.Polarity[0], 0.8f), Palette.Lit(Palette.Polarity[1], 0.8f) };
            _orbMats  = new[] { Palette.Lit(Palette.Polarity[0], 1.2f), Palette.Lit(Palette.Polarity[1], 1.2f) };
            _playerMats = new[] { Palette.Lit(Palette.Polarity[0], 1f), Palette.Lit(Palette.Polarity[1], 1f) };
            _wallMat = Palette.Lit(Palette.Wall, 0f);
            _ringMat = Palette.Lit(new Color(0.3f, 0.25f, 0.5f), 0.3f);

            BuildTrack();

            // Jogador: esfera + "anel" achatado para dar silhueta
            var p = Palette.Prim(PrimitiveType.Sphere, _playerMats[0]);
            p.name = "Player";
            p.transform.localScale = Vector3.one * 0.9f;
            p.transform.position = new Vector3(0f, 0.6f, 0f);
            _player = p.transform;
            _playerRenderer = p.GetComponent<Renderer>();
            var halo = Palette.Prim(PrimitiveType.Cylinder, _ringMat, _player);
            halo.transform.localScale = new Vector3(1.6f, 0.03f, 1.6f);
            _cam.Target = _player;

            _fx = new GameObject("FX").AddComponent<Fx>();
            _audio = gameObject.AddComponent<ProceduralAudio>();
            _hud = Hud.Build();

            EnterMenu();
        }

        private void BuildTrack()
        {
            var floor = Palette.Prim(PrimitiveType.Cube, Palette.Lit(new Color(0.08f, 0.07f, 0.14f), 0f));
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.1f, 50f);
            // Um pouco mais larga que as três pistas, ainda dentro dos arcos.
            floor.transform.localScale = new Vector3(LaneWidth * 3.5f, 0.2f, 140f);

            // Linhas das pistas
            var lineMat = Palette.Lit(new Color(0.4f, 0.3f, 0.8f), 1f);
            for (int i = -1; i <= 1; i += 2)
            {
                var l = Palette.Prim(PrimitiveType.Cube, lineMat);
                l.transform.position = new Vector3(i * LaneWidth * 0.5f, 0.01f, 50f);
                l.transform.localScale = new Vector3(0.05f, 0.02f, 140f);
            }

            // Arcos decorativos logo por fora das pistas, para caberem no quadro retrato.
            float archHalf = LaneWidth * 1.72f;
            for (int i = 0; i < 12; i++)
            {
                var ring = new GameObject("Arch").transform;
                ring.position = new Vector3(0f, 0f, i * 8f);
                for (int s = -1; s <= 1; s += 2)
                {
                    var pillar = Palette.Prim(PrimitiveType.Cube, _ringMat, ring);
                    pillar.transform.localPosition = new Vector3(s * archHalf, 1.8f, 0f);
                    pillar.transform.localScale = new Vector3(0.25f, 3.6f, 0.25f);
                }
                var top = Palette.Prim(PrimitiveType.Cube, _ringMat, ring);
                top.transform.localPosition = new Vector3(0f, 3.6f, 0f);
                top.transform.localScale = new Vector3(archHalf * 2f + 0.28f, 0.25f, 0.25f);
                _rings.Add(ring);
            }
        }

        // ---------------------------------------------------------------- estados
        private void EnterMenu()
        {
            _state = State.Menu;
            _hud.Score.text = $"BEST {_best}";
            _hud.Combo.text = "";
            _hud.Center.text =
                "<size=72><b><color=#1AF2FF>PRISMA</color> <color=#FF26B3>DASH</color></b></size>\n\n" +
                "Gates only let <b>your color</b> through.\n" +
                "Dodge the dark walls.\n" +
                "Grab orbs that match your color.\n\n" +
                "Tap the <b>left</b> or <b>right</b> side to change lane,\n" +
                "or swipe sideways.\n" +
                "Tap the <b>center</b>, or swipe up or down,\n" +
                "to switch color.\n\n" +
                "<b>Tap to start</b>";
        }

        private void StartGame()
        {
            foreach (var e in _entities) Destroy(e.Go);
            _entities.Clear();
            _state = State.Playing;
            _lane = 0; _polarity = 0; _combo = 0;
            _score = 0f; _time = 0f; _spawnTimer = 0.8f; _grazePulse = 0f;
            _speed = StartSpeed;
            _player.gameObject.SetActive(true);
            _player.localScale = Vector3.one * 0.9f;
            _playerRenderer.sharedMaterial = _playerMats[0];
            _hud.Center.text = "";
            _hud.ClearFlash();
        }

        private void GameOver()
        {
            _state = State.GameOver;
            _gameOverAt = Time.time;
            _cam.Shake(1f);
            _fx.Burst(_player.position, Palette.Polarity[_polarity], 120, 11f);
            _fx.Burst(_player.position, Color.white, 40, 6f);
            _audio.Death();
            _player.gameObject.SetActive(false);
            _hud.ClearFlash();
            int s = Mathf.FloorToInt(_score);
            bool record = s > _best;
            if (record) { _best = s; PlayerPrefs.SetInt("prisma_best", _best); PlayerPrefs.Save(); }
            _hud.Center.text = $"<size=80><b>GAME OVER</b></size>\n\nScore: <b>{s}</b>\n" +
                               (record ? "<color=#FFE14D><b>NEW RECORD!</b></color>" : $"Best: {_best}") +
                               "\n\n<b>Tap to play again</b>";
        }

        // ---------------------------------------------------------------- loop
        private void Update()
        {
            float dt = Time.deltaTime;
            ReadInput(out int move, out bool toggle, out bool any);

            switch (_state)
            {
                case State.Menu:
                    AnimateWorld(dt, StartSpeed * 0.5f);
                    if (any) StartGame();
                    break;
                case State.GameOver:
                    AnimateWorld(dt, 3f);
                    MoveEntities(dt * 0.15f); // câmera lenta dramática
                    if (any && Time.time - _gameOverAt > 0.6f) StartGame();
                    break;
                case State.Playing:
                    Tick(dt, move, toggle);
                    break;
            }
        }

        private void Tick(float dt, int move, bool toggle)
        {
            _time += dt;
            float difficulty = Mathf.Clamp01(_time / 100f); // rampa de ~100 s até o máximo
            _speed = Mathf.Lerp(StartSpeed, MaxSpeed, difficulty);

            if (move != 0)
            {
                int nl = Mathf.Clamp(_lane + move, -1, 1);
                if (nl != _lane) { _lane = nl; _audio.Move(); }
            }
            if (toggle)
            {
                _polarity = 1 - _polarity;
                _playerRenderer.sharedMaterial = _playerMats[_polarity];
                _fx.Burst(_player.position, Palette.Polarity[_polarity], 14, 4f);
                _audio.Switch();
            }

            // Movimento suave entre pistas + "tilt"
            var pos = _player.position;
            float targetX = _lane * LaneWidth;
            float vx = (targetX - pos.x) * 14f;
            pos.x += vx * dt;
            pos.y = 0.6f + Mathf.Abs(Mathf.Sin(_time * 10f)) * 0.08f;
            _player.position = pos;
            _player.rotation = Quaternion.Euler(_time * _speed * 40f, 0f, -vx * 2.5f);

            _score += dt * _speed * 0.5f;
            AnimateWorld(dt, _speed);
            MoveEntities(dt);
            Spawn(dt, difficulty);
            Resolve();

            float grazeScale = 1f + 0.22f * _grazePulse;
            _player.localScale = Vector3.one * (0.9f * grazeScale);
            _grazePulse = Mathf.Max(0f, _grazePulse - dt * 2.5f);

            _hud.Score.text = $"{Mathf.FloorToInt(_score)}";
            _hud.Combo.text = _combo > 1 ? $"COMBO x{_combo}" : "";
        }

        // ---------------------------------------------------------------- input
        private void ReadInput(out int move, out bool toggle, out bool any)
        {
            move = 0; toggle = false;
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) move = -1;
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) move = 1;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) ||
                Input.GetKeyDown(KeyCode.UpArrow) || Input.GetMouseButtonDown(1)) toggle = true;

            bool pointer = false;
            bool playing = _state == State.Playing;
            // Mouse: clique esquerdo usa as mesmas faixas verticais do toque (só durante o jogo).
            if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
            {
                pointer = true;
                if (playing) ZoneTap(Input.mousePosition.x, ref move, ref toggle);
            }
            // Toque: swipe horizontal = pista; swipe vertical = cor; toque = faixa da tela.
            // O gesto que começa ou reinicia a partida não dirige no soltar.
            float minSwipeX = Mathf.Max(48f, Screen.width * 0.08f);
            float minSwipeY = Mathf.Max(48f, Screen.height * 0.06f);
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    _touchStart = t.position;
                    pointer = true;
                    if (!playing) _swallowTouch = true;
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    if (_swallowTouch)
                    {
                        _swallowTouch = false;
                        continue;
                    }
                    if (!playing) continue;
                    var d = t.position - _touchStart;
                    if (Mathf.Abs(d.x) > minSwipeX && Mathf.Abs(d.x) > Mathf.Abs(d.y)) move = d.x > 0 ? 1 : -1;
                    else if (Mathf.Abs(d.y) > minSwipeY) toggle = true;
                    else ZoneTap(t.position.x, ref move, ref toggle);
                }
            }
            any = pointer || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        }

        private static void ZoneTap(float x, ref int move, ref bool toggle)
        {
            float f = x / Mathf.Max(1, Screen.width);
            if (f < 0.33f) move = -1;
            else if (f > 0.67f) move = 1;
            else toggle = true;
        }

        // ---------------------------------------------------------------- mundo
        private void AnimateWorld(float dt, float speed)
        {
            foreach (var r in _rings)
            {
                var p = r.position;
                p.z -= speed * dt;
                // Recicla atrás da câmera (ela fica em z ≈ -11 no retrato).
                if (p.z < -16f) p.z += _rings.Count * 8f;
                r.position = p;
            }
            _light.color = Color.Lerp(Color.white, Palette.Polarity[_polarity], 0.25f);
        }

        private void MoveEntities(float dt)
        {
            for (int i = _entities.Count - 1; i >= 0; i--)
            {
                var e = _entities[i];
                var t = e.Go.transform;
                t.position += Vector3.back * _speed * dt;
                if (e.Kind == Kind.Orb) t.Rotate(0f, 180f * dt, 90f * dt);
                if (t.position.z < -12f) { Destroy(e.Go); _entities.RemoveAt(i); }
            }
        }

        private void Spawn(float dt, float difficulty)
        {
            _spawnTimer -= dt;
            if (_spawnTimer > 0f) return;
            // Intervalo em DISTÂNCIA cai com a dificuldade; convertido em tempo pela velocidade
            float gap = Mathf.Lerp(22f, 11f, difficulty);
            _spawnTimer = gap / _speed;

            float r = Random.value;
            if (r < 0.40f)
            {
                SpawnGate(Random.Range(0, 2));
                // Na dificuldade alta, portais vêm com parede junto (decisão dupla!)
                if (Random.value < difficulty * 0.8f) SpawnWall(Random.Range(-1, 2), SpawnZ - 0.6f);
            }
            else if (r < 0.75f)
            {
                int free = Random.Range(-1, 2);
                for (int l = -1; l <= 1; l++)
                    if (l != free && (Random.value < 0.5f + difficulty * 0.4f)) SpawnWall(l, SpawnZ);
                SpawnOrb(free, Random.Range(0, 2), SpawnZ);
            }
            else
            {
                int lane = Random.Range(-1, 2), col = Random.Range(0, 2);
                for (int k = 0; k < 4; k++) SpawnOrb(lane, col, SpawnZ + k * 2.2f);
            }
        }

        private void SpawnGate(int color)
        {
            var root = new GameObject("Gate");
            root.transform.position = new Vector3(0f, 0f, SpawnZ);
            // Painel translúcido visual = barras horizontais finas + moldura
            for (int i = 0; i < 6; i++)
            {
                var bar = Palette.Prim(PrimitiveType.Cube, _gateMats[color], root.transform);
                bar.transform.localPosition = new Vector3(0f, 0.3f + i * 0.5f, 0f);
                bar.transform.localScale = new Vector3(LaneWidth * 3.2f, 0.12f, 0.12f);
            }
            for (int s = -1; s <= 1; s += 2)
            {
                var post = Palette.Prim(PrimitiveType.Cube, _gateMats[color], root.transform);
                post.transform.localPosition = new Vector3(s * LaneWidth * 1.6f, 1.6f, 0f);
                post.transform.localScale = new Vector3(0.3f, 3.2f, 0.3f);
            }
            _entities.Add(new Entity { Go = root, Kind = Kind.Gate, Lane = 99, Color = color });
        }

        private void SpawnWall(int lane, float z)
        {
            var w = Palette.Prim(PrimitiveType.Cube, _wallMat);
            w.name = "Wall";
            w.transform.position = new Vector3(lane * LaneWidth, 1f, z);
            w.transform.localScale = new Vector3(LaneWidth * 0.9f, 2f, 0.8f);
            _entities.Add(new Entity { Go = w, Kind = Kind.Wall, Lane = lane, Color = -1 });
        }

        private void SpawnOrb(int lane, int color, float z)
        {
            var o = Palette.Prim(PrimitiveType.Cube, _orbMats[color]);
            o.name = "Orb";
            o.transform.position = new Vector3(lane * LaneWidth, 0.8f, z);
            o.transform.localScale = Vector3.one * 0.45f;
            o.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
            _entities.Add(new Entity { Go = o, Kind = Kind.Orb, Lane = lane, Color = color });
        }

        /// <summary>Verifica, quando a entidade cruza z≈0, se o jogador sobrevive/pontua.</summary>
        private void Resolve()
        {
            // Pista "efetiva": onde a esfera está de fato (permite desvio no último instante).
            int playerLane = Mathf.RoundToInt(_player.position.x / LaneWidth);
            int grazePoints = 0;
            var grazeAt = _player.position;

            foreach (var e in _entities)
            {
                if (e.Done) continue;
                float z = e.Go.transform.position.z;
                if (z > 0.4f) continue;
                e.Done = true;

                switch (e.Kind)
                {
                    case Kind.Gate:
                        if (e.Color != _polarity) { GameOver(); return; }
                        _combo++;
                        _score += 10 * _combo;
                        _cam.Shake(0.25f);
                        _fx.Burst(_player.position + Vector3.forward, Palette.Polarity[e.Color], 50, 9f);
                        _audio.Pass(1f + Mathf.Min(_combo, 10) * 0.05f);
                        break;
                    case Kind.Wall:
                        if (e.Lane == playerLane) { GameOver(); return; }
                        if (IsGraze(e))
                        {
                            grazePoints += GrazeBonus();
                            grazeAt = e.Go.transform.position;
                        }
                        break;
                    case Kind.Orb:
                        if (e.Lane != playerLane) break;
                        if (e.Color == _polarity)
                        {
                            _combo++;
                            _score += 5 * _combo;
                            _fx.Burst(e.Go.transform.position, Palette.Polarity[e.Color], 20, 5f);
                            _audio.Orb(_combo);
                            e.Go.SetActive(false);
                        }
                        else
                        {
                            _combo = 0; // orbe da cor errada quebra o combo
                            _cam.Shake(0.35f);
                            _fx.Burst(e.Go.transform.position, Color.gray, 12, 3f);
                            e.Go.SetActive(false);
                        }
                        break;
                }
            }

            if (grazePoints > 0) AwardGraze(grazePoints, grazeAt);
        }

        /// <summary>
        /// Perto o bastante da parede para ter sido um desvio tardio.
        /// A colisão (mesma pista) já foi tratada antes desta checagem.
        /// </summary>
        private bool IsGraze(Entity wall)
        {
            float dist = Mathf.Abs(_player.position.x - wall.Lane * LaneWidth);
            return dist < LaneWidth * GrazeReach;
        }

        private int GrazeBonus()
        {
            float difficulty = Mathf.Clamp01(_time / 100f);
            return Mathf.RoundToInt(Mathf.Lerp(12f, 28f, difficulty));
        }

        private void AwardGraze(int points, Vector3 wallPos)
        {
            _score += points;
            _grazePulse = 1f;
            _cam.Shake(0.16f);
            var at = Vector3.Lerp(_player.position, wallPos, 0.45f);
            _fx.Burst(at, new Color(1f, 0.88f, 0.35f), 24, 7f);
            _audio.Graze();
            _hud.ShowFlash($"GRAZE  +{points}", new Color(1f, 0.86f, 0.28f));
        }
    }
}
