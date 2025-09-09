using UnityEngine;
using System.Collections.Generic;

namespace GamePieces
{
    /// <summary>
    /// ゲームボード管理クラス（シングルトン）
    /// </summary>
    public class GameBoard : MonoBehaviour
    {
        private static GameBoard instance;
        public static GameBoard Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GameBoard>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GameBoard");
                        instance = go.AddComponent<GameBoard>();
                    }
                }
                return instance;
            }
        }
        
        [Header("ボード設定")]
        [SerializeField] private int boardWidth = 10;
        [SerializeField] private int boardHeight = 10;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Vector3 boardOrigin = Vector3.zero;
        
        // ボード上の駒を管理
        private Dictionary<Vector2Int, BasePiece> piecesOnBoard = new Dictionary<Vector2Int, BasePiece>();
        
        // タイルの占領状態
        private Dictionary<Vector2Int, Team> tileOwnership = new Dictionary<Vector2Int, Team>();
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            InitializeBoard();
        }
        
        /// <summary>
        /// ボードの初期化
        /// </summary>
        private void InitializeBoard()
        {
            piecesOnBoard.Clear();
            tileOwnership.Clear();
            
            // すべてのタイルを中立で初期化
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    tileOwnership[new Vector2Int(x, y)] = Team.Neutral;
                }
            }
        }
        //どのタイルを最初の本拠地にするのかを決めることが出来るようにする　09/09
        
        /// <summary>
        /// 指定位置の駒を取得
        /// </summary>
        public BasePiece GetPieceAt(Vector2Int position)
        {
            return piecesOnBoard.TryGetValue(position, out BasePiece piece) ? piece : null;
        }
        
        /// <summary>
        /// 駒をボードに配置
        /// </summary>
        public bool PlacePiece(BasePiece piece, Vector2Int position)
        {
            if (!IsValidPosition(position))
                return false;
                
            if (piecesOnBoard.ContainsKey(position))//そこに既にコマがあるのか？を確かめているというわけ
                return false;
            
            piecesOnBoard[position] = piece;
            piece.transform.position = GetWorldPosition(position);
            return true;
        }
        
        /// <summary>
        /// 駒をボードから削除
        /// </summary>
        public void RemovePiece(Vector2Int position)
        {
            piecesOnBoard.Remove(position);
        }
        
        /// <summary>
        /// 駒の位置を更新
        /// </summary>
        public void UpdatePiecePosition(BasePiece piece, Vector2Int oldPosition, Vector2Int newPosition)
        {
            if (piecesOnBoard.TryGetValue(oldPosition, out BasePiece existingPiece) && existingPiece == piece)
            {
                piecesOnBoard.Remove(oldPosition);
                piecesOnBoard[newPosition] = piece;
            }
        }
        
        /// <summary>
        /// 位置が有効かチェック
        /// </summary>
        public bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < boardWidth &&
                   position.y >= 0 && position.y < boardHeight;
        }
        
        /// <summary>
        /// グリッド座標をワールド座標に変換
        /// </summary>
        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            return boardOrigin + new Vector3(
                gridPosition.x * tileSize,
                0,
                gridPosition.y * tileSize
            );
        }
        
        /// <summary>
        /// ワールド座標をグリッド座標に変換
        /// </summary>
        public Vector2Int GetGridPosition(Vector3 worldPosition)
        {
            Vector3 localPos = worldPosition - boardOrigin;
            return new Vector2Int(
                Mathf.RoundToInt(localPos.x / tileSize),
                Mathf.RoundToInt(localPos.z / tileSize)
            );
        }
        
        /// <summary>
        /// 2点間の経路が通行可能かチェック（駒を飛び越えられない）
        /// </summary>
        public bool IsPathClear(Vector2Int start, Vector2Int end)
        {
            // 簡単な直線経路チェック
            // /（後で A*に作り直す）
            int dx = Mathf.Abs(end.x - start.x);
            int dy = Mathf.Abs(end.y - start.y);
            int sx = start.x < end.x ? 1 : -1;
            int sy = start.y < end.y ? 1 : -1;
            int err = dx - dy;
            
            Vector2Int current = start;
            
            while (current != end)
            {
                int e2 = 2 * err;
                
                if (e2 > -dy)
                {
                    err -= dy;
                    current.x += sx;
                }
                
                if (e2 < dx)
                {
                    err += dx;
                    current.y += sy;
                }
                
                // 経路上に駒がある場合は通行不可
                if (current != end && piecesOnBoard.ContainsKey(current))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 指定位置が特定チームの領土かチェック
        /// </summary>
        public bool IsTerritory(Vector2Int position, Team team)
        {
            return tileOwnership.TryGetValue(position, out Team owner) && owner == team;
        }
        
        /// <summary>
        /// タイルの占領状態を設定
        /// </summary>
        public void SetTileOwnership(Vector2Int position, Team team)
        {
            if (IsValidPosition(position))
            {
                tileOwnership[position] = team;
            }
        }
        
        /// <summary>
        /// 隣接するマスを取得
        /// </summary>
        public List<Vector2Int> GetAdjacentPositions(Vector2Int position)
        {
            List<Vector2Int> adjacent = new List<Vector2Int>();
            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };
            
            foreach (var dir in directions)
            {
                Vector2Int newPos = position + dir;
                if (IsValidPosition(newPos))
                {
                    adjacent.Add(newPos);
                }
            }
            
            return adjacent;
        }
    }
}