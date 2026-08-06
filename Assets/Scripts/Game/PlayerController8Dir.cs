using UnityEngine;

namespace Game
{
    /// <summary>
    /// 8-directional top-down player controller.
    /// Sprite sheet layout (walk): 8 columns x 5 rows
    ///   Row 0 = Down (front), Row 1 = Down-Right, Row 2 = Right,
    ///   Row 3 = Up-Right, Row 4 = Up (back)
    ///   Left directions mirrored via flipX on rows 1-3.
    /// Sprite sheet layout (idle): 1 column x 5 rows (same row order)
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController8Dir : MonoBehaviour
    {
        [Header("Sprites")]
        public Texture2D walkSheet;
        public Texture2D idleSheet;
        public int walkCols = 8;
        public int walkRows = 5;
        public int idleCols = 1;
        public int idleRows = 5;
        [Tooltip("Walk sheet pixels-per-unit")]
        public float walkPpu = 100f;
        [Tooltip("Idle sheet pixels-per-unit (auto-calculated to match walk character size)")]
        public float idlePpu = 172f;

        [Header("Animation")]
        public float walkFps = 10f;
        public float idleFps = 2f;

        [Header("Movement")]
        public float moveSpeed = 5f;

        private SpriteRenderer _sr;
        private Sprite[][] _walkSprites;
        private Sprite[] _idleSprites;
        private int _currentDir = 0;
        private int _currentFrame = 0;
        private float _timer = 0f;
        private bool _isMoving = false;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            LoadSprites();
        }

        void LoadSprites()
        {
            // Walk sprites: 5 rows x 8 cols
            _walkSprites = new Sprite[5][];
            if (walkSheet != null)
            {
                int cw = walkSheet.width / walkCols;
                int ch = walkSheet.height / walkRows;
                for (int row = 0; row < 5; row++)
                {
                    _walkSprites[row] = new Sprite[walkCols];
                    for (int col = 0; col < walkCols; col++)
                    {
                        float y = (walkRows - 1 - row) * ch;
                        _walkSprites[row][col] = Sprite.Create(
                            walkSheet,
                            new Rect(col * cw, y, cw, ch),
                            new Vector2(0.5f, 0.1f),
                            walkPpu
                        );
                    }
                }
            }

            // Idle sprites: 5 rows x 1 col, use different PPU to match character size
            _idleSprites = new Sprite[5];
            if (idleSheet != null)
            {
                int cw = idleSheet.width / idleCols;
                int ch = idleSheet.height / idleRows;
                for (int row = 0; row < 5; row++)
                {
                    float y = (idleRows - 1 - row) * ch;
                    _idleSprites[row] = Sprite.Create(
                        idleSheet,
                        new Rect(0, y, cw, ch),
                        new Vector2(0.5f, 0.1f),
                        idlePpu
                    );
                }
            }
        }

        void Update()
        {
            float h = 0, v = 0;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1;

            _isMoving = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;

            if (_isMoving)
            {
                _currentDir = GetDirectionIndex(h, v);

                Vector2 dir = new Vector2(h, v).normalized;
                transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);

                _timer += Time.deltaTime;
                if (_timer >= 1f / walkFps)
                {
                    _timer = 0;
                    _currentFrame = (_currentFrame + 1) % walkCols;
                }

                // Map direction to sprite row + flip
                // Sprite sheet rows face LEFT: 0=Down, 1=DownLeft, 2=Left, 3=UpLeft, 4=Up
                // Right directions need flipX=true
                int row;
                bool flip;
                switch (_currentDir)
                {
                    case 0: row = 0; flip = false; break; // Down
                    case 1: row = 1; flip = true;  break; // DownRight
                    case 2: row = 2; flip = true;  break; // Right
                    case 3: row = 3; flip = true;  break; // UpRight
                    case 4: row = 4; flip = false; break; // Up
                    case 5: row = 3; flip = false; break; // UpLeft
                    case 6: row = 2; flip = false; break; // Left
                    case 7: row = 1; flip = false; break; // DownLeft
                    default: row = 0; flip = false; break;
                }

                if (_walkSprites != null && _walkSprites[row] != null)
                {
                    _sr.sprite = _walkSprites[row][_currentFrame];
                    _sr.flipX = flip;
                }
            }
            else
            {
                _timer += Time.deltaTime;
                if (_timer >= 1f / idleFps)
                {
                    _timer = 0;
                    _currentFrame = (_currentFrame + 1) % idleCols;
                }

                int row;
                bool flip;
                switch (_currentDir)
                {
                    case 0: row = 0; flip = false; break;
                    case 1: row = 1; flip = true;  break;
                    case 2: row = 2; flip = true;  break;
                    case 3: row = 3; flip = true;  break;
                    case 4: row = 4; flip = false; break;
                    case 5: row = 3; flip = false; break;
                    case 6: row = 2; flip = false; break;
                    case 7: row = 1; flip = false; break;
                    default: row = 0; flip = false; break;
                }

                if (_idleSprites != null && _idleSprites[row] != null)
                {
                    _sr.sprite = _idleSprites[row];
                    _sr.flipX = flip;
                }
            }

            // Dynamic Y-sort: player uses 5000+range, objects use 0-4999 range
            // Ground is always 0. Objects sort by their Y too.
            // Player at Y=0 → sortingOrder=5000, above ground(0) and most objects
            // Player at Y=5 (north) → sortingOrder=4950, behind south objects
            _sr.sortingOrder = 5000 + Mathf.RoundToInt(-transform.position.y * 10);
        }

        int GetDirectionIndex(float h, float v)
        {
            if (h == 0 && v < 0) return 0;  // Down
            if (h > 0 && v < 0) return 1;    // DownRight
            if (h > 0 && v == 0) return 2;   // Right
            if (h > 0 && v > 0) return 3;    // UpRight
            if (h == 0 && v > 0) return 4;   // Up
            if (h < 0 && v > 0) return 5;     // UpLeft
            if (h < 0 && v == 0) return 6;    // Left
            if (h < 0 && v < 0) return 7;     // DownLeft
            return 0;
        }
    }
}
