using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GamePieces
{
    /// <summary>
    /// ゲーム全体を管理するマネージャークラス（シングルトン）
    /// すべての駒と盤面の相互作用を仲介する
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region シングルトン

        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GameManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        instance = go.AddComponent<GameManager>();
                    }
                }
                return instance;
            }
        }

        #endregion

        #region ゲーム状態

        public enum GameState
        {
            Initializing,    // 初期化中
            WaitingForInput, // プレイヤー入力待ち
            ProcessingTurn,  // ターン処理中
            AnimatingAction, // アニメーション再生中
            GameOver,        // ゲーム終了
            Paused          // 一時停止
        }

        [Header("ゲーム状態")]
        [SerializeField] private GameState currentState = GameState.Initializing;
        [SerializeField] private Team currentTurn = Team.Player;
        [SerializeField] private int turnCount = 0;

        #endregion

        #region リソース管理

        [Header("リソース設定")]
        [SerializeField] private Dictionary<Team, int> teamResources = new Dictionary<Team, int>();
        [SerializeField] private Dictionary<Team, int> teamPopulation = new Dictionary<Team, int>();
        [SerializeField] private Dictionary<Team, int> maxPopulation = new Dictionary<Team, int>();

        [SerializeField] private int startingResources = 100;
        [SerializeField] private int startingMaxPopulation = 20;
        [SerializeField] private int resourcesPerTurn = 10;

        #endregion

        #region 駒管理

        [Header("駒管理")]
        private Dictionary<Team, List<BasePiece>> teamPieces = new Dictionary<Team, List<BasePiece>>();
        private BasePiece selectedPiece = null;

        #endregion

        #region プロパティ

        public GameState CurrentState => currentState;
        public Team CurrentTurn => currentTurn;
        public int TurnCount => turnCount;
        public BasePiece SelectedPiece => selectedPiece;

        #endregion

        #region イベント

        public event Action<GameState, GameState> OnGameStateChanged;
        public event Action<Team> OnTurnChanged;
        public event Action<Team, int> OnResourcesChanged;
        public event Action<Team, int, int> OnPopulationChanged;
        public event Action<BasePiece> OnPieceSelected;
        public event Action<BasePiece> OnPieceDeselected;
        public event Action<Team> OnGameOver;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGame();
        }

        private void Start()
        {
            StartGame();
        }

        #endregion

        #region 初期化

        /// <summary>
        /// ゲームの初期化
        /// </summary>
        private void InitializeGame()
        {
            // チーム毎のリソース初期化
            foreach (Team team in Enum.GetValues(typeof(Team)))
            {
                teamResources[team] = startingResources;
                teamPopulation[team] = 0;
                maxPopulation[team] = startingMaxPopulation;
                teamPieces[team] = new List<BasePiece>();
            }

            // GameBoardの初期化を確認
            if (GameBoard.Instance == null)
            {
                Debug.LogError("GameBoard not found!");
            }
        }

        /// <summary>
        /// ゲーム開始
        /// </summary>
        public void StartGame()
        {
            ChangeGameState(GameState.WaitingForInput);
            StartTurn(Team.Player);
        }

        #endregion

        #region ゲーム状態管理

        /// <summary>
        /// ゲーム状態を変更
        /// </summary>
        private void ChangeGameState(GameState newState)
        {
            GameState oldState = currentState;
            currentState = newState;
            OnGameStateChanged?.Invoke(oldState, newState);
        }

        #endregion

        #region ターン管理

        /// <summary>
        /// ターン開始
        /// </summary>
        private void StartTurn(Team team)
        {
            currentTurn = team;
            turnCount++;

            // リソース収入
            AddResources(team, resourcesPerTurn);

            // 全駒の行動力回復（完全回復）
            foreach (var piece in teamPieces[team])
            {
                piece.RestoreActionPoints();
            }

            OnTurnChanged?.Invoke(team);

            if (team == Team.Player)
            {
                ChangeGameState(GameState.WaitingForInput);
            }
            else
            {
                // AI処理
                StartCoroutine(ProcessAITurn(team));
            }
        }

        /// <summary>
        /// ターン終了
        /// </summary>
        public void EndTurn()
        {
            if (currentState != GameState.WaitingForInput)
                return;

            ChangeGameState(GameState.ProcessingTurn);

            // 次のチームへ
            Team nextTeam = GetNextTeam(currentTurn);
            StartTurn(nextTeam);
        }

        /// <summary>
        /// 次のチームを取得
        /// </summary>
        private Team GetNextTeam(Team current)
        {
            // 簡単な実装：Player -> Enemy -> Player
            if (current == Team.Player)
                return Team.Enemy;
            else
                return Team.Player;
        }

        /// <summary>
        /// AIターン処理
        /// </summary>
        private IEnumerator ProcessAITurn(Team team)
        {
            ChangeGameState(GameState.ProcessingTurn);
            yield return new WaitForSeconds(1f);

            // TODO: AI実装

            EndTurn();
        }

        #endregion

        #region 駒操作インターフェース

        /// <summary>
        /// 駒を生成（GameBoardへの配置も含む）
        /// </summary>
        public BasePiece SpawnPiece(GameObject piecePrefab, Vector2Int position, Team team)
        {
            // リソースとポピュレーションチェック
            BasePiece pieceComponent = piecePrefab.GetComponent<BasePiece>();
            if (pieceComponent == null)
                return null;

            int cost = pieceComponent.GetResourceCost();
            int popCost = pieceComponent.GetPopulationCost();

            if (!CanAfford(team, cost, popCost))
                return null;

            // 配置可能かチェック
            if (!CanPlacePieceAt(position, team))
                return null;

            // 駒を生成
            GameObject pieceObj = Instantiate(piecePrefab);
            BasePiece piece = pieceObj.GetComponent<BasePiece>();

            // 駒を初期化
            piece.Initialize(team, position);

            // GameBoardに配置
            if (!GameBoard.Instance.PlacePiece(piece, position))
            {
                Destroy(pieceObj);
                return null;
            }

            // リソース消費
            ConsumeResources(team, cost, popCost);

            // チームリストに追加
            teamPieces[team].Add(piece);

            // イベント登録
            piece.OnPieceDeath += HandlePieceDeath;
            piece.OnPositionChanged += HandlePiecePositionChanged;

            return piece;
        }

        /// <summary>
        /// 駒を選択
        /// </summary>
        public void SelectPiece(BasePiece piece)
        {
            if (piece == null || piece.OwnerTeam != currentTurn)
                return;

            if (currentState != GameState.WaitingForInput)
                return;

            if (selectedPiece != null)
            {
                OnPieceDeselected?.Invoke(selectedPiece);
            }

            selectedPiece = piece;
            OnPieceSelected?.Invoke(piece);
        }

        /// <summary>
        /// 駒の選択解除
        /// </summary>
        public void DeselectPiece()
        {
            if (selectedPiece != null)
            {
                OnPieceDeselected?.Invoke(selectedPiece);
                selectedPiece = null;
            }
        }

        /// <summary>
        /// 駒を移動（GameManagerを通じて）
        /// </summary>
        public bool MovePiece(BasePiece piece, Vector2Int targetPosition)
        {
            if (currentState != GameState.WaitingForInput)
                return false;

            if (piece.OwnerTeam != currentTurn)
                return false;

            // 移動可能性チェック
            if (!CanMovePieceTo(piece, targetPosition))
                return false;

            // アニメーション状態へ
            ChangeGameState(GameState.AnimatingAction);

            // 移動実行を駒に委譲
            bool success = piece.TryMove(targetPosition);

            // 入力待ち状態へ戻す
            StartCoroutine(ReturnToInputState());

            return success;
        }

        /// <summary>
        /// 駒で攻撃（GameManagerを通じて）
        /// </summary>
        public bool AttackWithPiece(BasePiece attacker, BasePiece target)
        {
            if (currentState != GameState.WaitingForInput)
                return false;

            if (attacker.OwnerTeam != currentTurn)
                return false;

            if (attacker.OwnerTeam == target.OwnerTeam)
                return false;

            // アニメーション状態へ
            ChangeGameState(GameState.AnimatingAction);

            // 攻撃実行を駒に委譲
            bool success = attacker.TryAttack(target);

            // 入力待ち状態へ戻す
            StartCoroutine(ReturnToInputState());

            return success;
        }

        /// <summary>
        /// アニメーション後、入力待ち状態へ戻る
        /// </summary>
        private IEnumerator ReturnToInputState()
        {
            yield return new WaitForSeconds(0.5f);
            if (currentTurn == Team.Player)
            {
                ChangeGameState(GameState.WaitingForInput);
            }
        }

        #endregion

        #region 検証メソッド

        /// <summary>
        /// 指定位置に駒を配置可能か
        /// </summary>
        public bool CanPlacePieceAt(Vector2Int position, Team team)
        {
            // 有効な位置か
            if (!GameBoard.Instance.IsValidPosition(position))
                return false;

            // 既に駒があるか
            if (GameBoard.Instance.GetPieceAt(position) != null)
                return false;

            // 自チームの領土または隣接しているか
            if (!GameBoard.Instance.IsTerritory(position, team))
            {
                // 隣接チェック
                var adjacentPositions = GameBoard.Instance.GetAdjacentPositions(position);
                bool hasAdjacentAlly = false;
                foreach (var adj in adjacentPositions)
                {
                    BasePiece adjPiece = GameBoard.Instance.GetPieceAt(adj);
                    if (adjPiece != null && adjPiece.OwnerTeam == team)
                    {
                        hasAdjacentAlly = true;
                        break;
                    }
                }

                if (!hasAdjacentAlly)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 駒を指定位置へ移動可能か
        /// </summary>
        public bool CanMovePieceTo(BasePiece piece, Vector2Int targetPosition)
        {
            // 基本的な検証は駒クラスに委譲
            // ここではゲーム全体のルールをチェック

            // ゲーム状態チェック
            if (currentState != GameState.WaitingForInput)
                return false;

            // ターンチェック
            if (piece.OwnerTeam != currentTurn)
                return false;

            return true;
        }

        #endregion

        #region リソース管理

        /// <summary>
        /// リソースを追加
        /// </summary>
        public void AddResources(Team team, int amount)
        {
            teamResources[team] += amount;
            OnResourcesChanged?.Invoke(team, teamResources[team]);
        }

        /// <summary>
        /// リソースとポピュレーションを消費
        /// </summary>
        private void ConsumeResources(Team team, int resourceCost, int populationCost)
        {
            teamResources[team] -= resourceCost;
            teamPopulation[team] += populationCost;

            OnResourcesChanged?.Invoke(team, teamResources[team]);
            OnPopulationChanged?.Invoke(team, teamPopulation[team], maxPopulation[team]);
        }

        /// <summary>
        /// リソースが足りるかチェック
        /// </summary>
        public bool CanAfford(Team team, int resourceCost, int populationCost)
        {
            if (teamResources[team] < resourceCost)
                return false;

            if (teamPopulation[team] + populationCost > maxPopulation[team])
                return false;

            return true;
        }

        /// <summary>
        /// 現在のリソース取得
        /// </summary>
        public int GetResources(Team team)
        {
            return teamResources.TryGetValue(team, out int resources) ? resources : 0;
        }

        /// <summary>
        /// 現在の人口取得
        /// </summary>
        public (int current, int max) GetPopulation(Team team)
        {
            int current = teamPopulation.TryGetValue(team, out int pop) ? pop : 0;
            int max = maxPopulation.TryGetValue(team, out int maxPop) ? maxPop : 0;
            return (current, max);
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// 駒死亡時の処理
        /// </summary>
        private void HandlePieceDeath(BasePiece piece)
        {
            // チームリストから削除
            teamPieces[piece.OwnerTeam].Remove(piece);

            // ポピュレーション回復
            teamPopulation[piece.OwnerTeam] -= piece.GetPopulationCost();
            OnPopulationChanged?.Invoke(piece.OwnerTeam, teamPopulation[piece.OwnerTeam], maxPopulation[piece.OwnerTeam]);

            // GameBoardから削除
            GameBoard.Instance.RemovePiece(piece.CurrentPosition);

            // 選択中だった場合は選択解除
            if (selectedPiece == piece)
            {
                DeselectPiece();
            }

            // 勝利条件チェック
            CheckVictoryConditions();
        }

        /// <summary>
        /// 駒位置変更時の処理
        /// </summary>
        private void HandlePiecePositionChanged(Vector2Int oldPosition, Vector2Int newPosition)
        {
            // GameBoardの位置情報を更新
            BasePiece piece = GameBoard.Instance.GetPieceAt(oldPosition);
            if (piece != null)
            {
                GameBoard.Instance.UpdatePiecePosition(piece, oldPosition, newPosition);
            }
        }

        #endregion

        #region 勝利条件

        /// <summary>
        /// 勝利条件をチェック
        /// </summary>
        private void CheckVictoryConditions()
        {
            // 敵チームの駒が0になったか
            foreach (Team team in Enum.GetValues(typeof(Team)))
            {
                if (team == Team.Neutral)
                    continue;

                if (teamPieces[team].Count == 0)
                {
                    // 敗北したチームの反対が勝利
                    Team winner = team == Team.Player ? Team.Enemy : Team.Player;
                    EndGame(winner);
                    return;
                }
            }

            // その他の勝利条件をここに追加
        }

        /// <summary>
        /// ゲーム終了
        /// </summary>
        private void EndGame(Team winner)
        {
            ChangeGameState(GameState.GameOver);
            OnGameOver?.Invoke(winner);
        }

        #endregion

        #region ユーティリティ

        /// <summary>
        /// 指定チームの全駒を取得
        /// </summary>
        public List<BasePiece> GetTeamPieces(Team team)
        {
            return new List<BasePiece>(teamPieces[team]);
        }

        /// <summary>
        /// 指定位置周辺の駒を取得
        /// </summary>
        public List<BasePiece> GetPiecesInRange(Vector2Int center, int range)
        {
            List<BasePiece> piecesInRange = new List<BasePiece>();

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    Vector2Int pos = center + new Vector2Int(x, y);
                    if (GameBoard.Instance.IsValidPosition(pos))
                    {
                        BasePiece piece = GameBoard.Instance.GetPieceAt(pos);
                        if (piece != null)
                        {
                            piecesInRange.Add(piece);
                        }
                    }
                }
            }

            return piecesInRange;
        }

        #endregion
    }
}