using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GamePieces
{
    /// <summary>
    /// �Q�[���S�̂��Ǘ�����}�l�[�W���[�N���X�i�V���O���g���j
    /// ���ׂĂ̋�ƔՖʂ̑��ݍ�p�𒇉��
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region �V���O���g��

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

        #region �Q�[�����

        public enum GameState
        {
            Initializing,    // ��������
            WaitingForInput, // �v���C���[���͑҂�
            ProcessingTurn,  // �^�[��������
            AnimatingAction, // �A�j���[�V�����Đ���
            GameOver,        // �Q�[���I��
            Paused          // �ꎞ��~
        }

        [Header("�Q�[�����")]
        [SerializeField] private GameState currentState = GameState.Initializing;
        [SerializeField] private Team currentTurn = Team.Player;
        [SerializeField] private int turnCount = 0;

        #endregion

        #region ���\�[�X�Ǘ�

        [Header("���\�[�X�ݒ�")]
        [SerializeField] private Dictionary<Team, int> teamResources = new Dictionary<Team, int>();
        [SerializeField] private Dictionary<Team, int> teamPopulation = new Dictionary<Team, int>();
        [SerializeField] private Dictionary<Team, int> maxPopulation = new Dictionary<Team, int>();

        [SerializeField] private int startingResources = 100;
        [SerializeField] private int startingMaxPopulation = 20;
        [SerializeField] private int resourcesPerTurn = 10;

        #endregion

        #region ��Ǘ�

        [Header("��Ǘ�")]
        private Dictionary<Team, List<BasePiece>> teamPieces = new Dictionary<Team, List<BasePiece>>();
        private BasePiece selectedPiece = null;

        #endregion

        #region �v���p�e�B

        public GameState CurrentState => currentState;
        public Team CurrentTurn => currentTurn;
        public int TurnCount => turnCount;
        public BasePiece SelectedPiece => selectedPiece;

        #endregion

        #region �C�x���g

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

        #region ������

        /// <summary>
        /// �Q�[���̏�����
        /// </summary>
        private void InitializeGame()
        {
            // �`�[�����̃��\�[�X������
            foreach (Team team in Enum.GetValues(typeof(Team)))
            {
                teamResources[team] = startingResources;
                teamPopulation[team] = 0;
                maxPopulation[team] = startingMaxPopulation;
                teamPieces[team] = new List<BasePiece>();
            }

            // GameBoard�̏��������m�F
            if (GameBoard.Instance == null)
            {
                Debug.LogError("GameBoard not found!");
            }
        }

        /// <summary>
        /// �Q�[���J�n
        /// </summary>
        public void StartGame()
        {
            ChangeGameState(GameState.WaitingForInput);
            StartTurn(Team.Player);
        }

        #endregion

        #region �Q�[����ԊǗ�

        /// <summary>
        /// �Q�[����Ԃ�ύX
        /// </summary>
        private void ChangeGameState(GameState newState)
        {
            GameState oldState = currentState;
            currentState = newState;
            OnGameStateChanged?.Invoke(oldState, newState);
        }

        #endregion

        #region �^�[���Ǘ�

        /// <summary>
        /// �^�[���J�n
        /// </summary>
        private void StartTurn(Team team)
        {
            currentTurn = team;
            turnCount++;

            // ���\�[�X����
            AddResources(team, resourcesPerTurn);

            // �S��̍s���͉񕜁i���S�񕜁j
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
                // AI����
                StartCoroutine(ProcessAITurn(team));
            }
        }

        /// <summary>
        /// �^�[���I��
        /// </summary>
        public void EndTurn()
        {
            if (currentState != GameState.WaitingForInput)
                return;

            ChangeGameState(GameState.ProcessingTurn);

            // ���̃`�[����
            Team nextTeam = GetNextTeam(currentTurn);
            StartTurn(nextTeam);
        }

        /// <summary>
        /// ���̃`�[�����擾
        /// </summary>
        private Team GetNextTeam(Team current)
        {
            // �ȒP�Ȏ����FPlayer -> Enemy -> Player
            if (current == Team.Player)
                return Team.Enemy;
            else
                return Team.Player;
        }

        /// <summary>
        /// AI�^�[������
        /// </summary>
        private IEnumerator ProcessAITurn(Team team)
        {
            ChangeGameState(GameState.ProcessingTurn);
            yield return new WaitForSeconds(1f);

            // TODO: AI����

            EndTurn();
        }

        #endregion

        #region ���C���^�[�t�F�[�X

        /// <summary>
        /// ��𐶐��iGameBoard�ւ̔z�u���܂ށj
        /// </summary>
        public BasePiece SpawnPiece(GameObject piecePrefab, Vector2Int position, Team team)
        {
            // ���\�[�X�ƃ|�s�����[�V�����`�F�b�N
            BasePiece pieceComponent = piecePrefab.GetComponent<BasePiece>();
            if (pieceComponent == null)
                return null;

            int cost = pieceComponent.GetResourceCost();
            int popCost = pieceComponent.GetPopulationCost();

            if (!CanAfford(team, cost, popCost))
                return null;

            // �z�u�\���`�F�b�N
            if (!CanPlacePieceAt(position, team))
                return null;

            // ��𐶐�
            GameObject pieceObj = Instantiate(piecePrefab);
            BasePiece piece = pieceObj.GetComponent<BasePiece>();

            // ���������
            piece.Initialize(team, position);

            // GameBoard�ɔz�u
            if (!GameBoard.Instance.PlacePiece(piece, position))
            {
                Destroy(pieceObj);
                return null;
            }

            // ���\�[�X����
            ConsumeResources(team, cost, popCost);

            // �`�[�����X�g�ɒǉ�
            teamPieces[team].Add(piece);

            // �C�x���g�o�^
            piece.OnPieceDeath += HandlePieceDeath;
            piece.OnPositionChanged += HandlePiecePositionChanged;

            return piece;
        }

        /// <summary>
        /// ���I��
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
        /// ��̑I������
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
        /// ����ړ��iGameManager��ʂ��āj
        /// </summary>
        public bool MovePiece(BasePiece piece, Vector2Int targetPosition)
        {
            if (currentState != GameState.WaitingForInput)
                return false;

            if (piece.OwnerTeam != currentTurn)
                return false;

            // �ړ��\���`�F�b�N
            if (!CanMovePieceTo(piece, targetPosition))
                return false;

            // �A�j���[�V������Ԃ�
            ChangeGameState(GameState.AnimatingAction);

            // �ړ����s����ɈϏ�
            bool success = piece.TryMove(targetPosition);

            // ���͑҂���Ԃ֖߂�
            StartCoroutine(ReturnToInputState());

            return success;
        }

        /// <summary>
        /// ��ōU���iGameManager��ʂ��āj
        /// </summary>
        public bool AttackWithPiece(BasePiece attacker, BasePiece target)
        {
            if (currentState != GameState.WaitingForInput)
                return false;

            if (attacker.OwnerTeam != currentTurn)
                return false;

            if (attacker.OwnerTeam == target.OwnerTeam)
                return false;

            // �A�j���[�V������Ԃ�
            ChangeGameState(GameState.AnimatingAction);

            // �U�����s����ɈϏ�
            bool success = attacker.TryAttack(target);

            // ���͑҂���Ԃ֖߂�
            StartCoroutine(ReturnToInputState());

            return success;
        }

        /// <summary>
        /// �A�j���[�V������A���͑҂���Ԃ֖߂�
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

        #region ���؃��\�b�h

        /// <summary>
        /// �w��ʒu�ɋ��z�u�\��
        /// </summary>
        public bool CanPlacePieceAt(Vector2Int position, Team team)
        {
            // �L���Ȉʒu��
            if (!GameBoard.Instance.IsValidPosition(position))
                return false;

            // ���ɋ���邩
            if (GameBoard.Instance.GetPieceAt(position) != null)
                return false;

            // ���`�[���̗̓y�܂��͗אڂ��Ă��邩
            if (!GameBoard.Instance.IsTerritory(position, team))
            {
                // �אڃ`�F�b�N
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
        /// ����w��ʒu�ֈړ��\��
        /// </summary>
        public bool CanMovePieceTo(BasePiece piece, Vector2Int targetPosition)
        {
            // ��{�I�Ȍ��؂͋�N���X�ɈϏ�
            // �����ł̓Q�[���S�̂̃��[�����`�F�b�N

            // �Q�[����ԃ`�F�b�N
            if (currentState != GameState.WaitingForInput)
                return false;

            // �^�[���`�F�b�N
            if (piece.OwnerTeam != currentTurn)
                return false;

            return true;
        }

        #endregion

        #region ���\�[�X�Ǘ�

        /// <summary>
        /// ���\�[�X��ǉ�
        /// </summary>
        public void AddResources(Team team, int amount)
        {
            teamResources[team] += amount;
            OnResourcesChanged?.Invoke(team, teamResources[team]);
        }

        /// <summary>
        /// ���\�[�X�ƃ|�s�����[�V����������
        /// </summary>
        private void ConsumeResources(Team team, int resourceCost, int populationCost)
        {
            teamResources[team] -= resourceCost;
            teamPopulation[team] += populationCost;

            OnResourcesChanged?.Invoke(team, teamResources[team]);
            OnPopulationChanged?.Invoke(team, teamPopulation[team], maxPopulation[team]);
        }

        /// <summary>
        /// ���\�[�X������邩�`�F�b�N
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
        /// ���݂̃��\�[�X�擾
        /// </summary>
        public int GetResources(Team team)
        {
            return teamResources.TryGetValue(team, out int resources) ? resources : 0;
        }

        /// <summary>
        /// ���݂̐l���擾
        /// </summary>
        public (int current, int max) GetPopulation(Team team)
        {
            int current = teamPopulation.TryGetValue(team, out int pop) ? pop : 0;
            int max = maxPopulation.TryGetValue(team, out int maxPop) ? maxPop : 0;
            return (current, max);
        }

        #endregion

        #region �C�x���g�n���h��

        /// <summary>
        /// ��S���̏���
        /// </summary>
        private void HandlePieceDeath(BasePiece piece)
        {
            // �`�[�����X�g����폜
            teamPieces[piece.OwnerTeam].Remove(piece);

            // �|�s�����[�V������
            teamPopulation[piece.OwnerTeam] -= piece.GetPopulationCost();
            OnPopulationChanged?.Invoke(piece.OwnerTeam, teamPopulation[piece.OwnerTeam], maxPopulation[piece.OwnerTeam]);

            // GameBoard����폜
            GameBoard.Instance.RemovePiece(piece.CurrentPosition);

            // �I�𒆂������ꍇ�͑I������
            if (selectedPiece == piece)
            {
                DeselectPiece();
            }

            // ���������`�F�b�N
            CheckVictoryConditions();
        }

        /// <summary>
        /// ��ʒu�ύX���̏���
        /// </summary>
        private void HandlePiecePositionChanged(Vector2Int oldPosition, Vector2Int newPosition)
        {
            // GameBoard�̈ʒu�����X�V
            BasePiece piece = GameBoard.Instance.GetPieceAt(oldPosition);
            if (piece != null)
            {
                GameBoard.Instance.UpdatePiecePosition(piece, oldPosition, newPosition);
            }
        }

        #endregion

        #region ��������

        /// <summary>
        /// �����������`�F�b�N
        /// </summary>
        private void CheckVictoryConditions()
        {
            // �G�`�[���̋0�ɂȂ�����
            foreach (Team team in Enum.GetValues(typeof(Team)))
            {
                if (team == Team.Neutral)
                    continue;

                if (teamPieces[team].Count == 0)
                {
                    // �s�k�����`�[���̔��΂�����
                    Team winner = team == Team.Player ? Team.Enemy : Team.Player;
                    EndGame(winner);
                    return;
                }
            }

            // ���̑��̏��������������ɒǉ�
        }

        /// <summary>
        /// �Q�[���I��
        /// </summary>
        private void EndGame(Team winner)
        {
            ChangeGameState(GameState.GameOver);
            OnGameOver?.Invoke(winner);
        }

        #endregion

        #region ���[�e�B���e�B

        /// <summary>
        /// �w��`�[���̑S����擾
        /// </summary>
        public List<BasePiece> GetTeamPieces(Team team)
        {
            return new List<BasePiece>(teamPieces[team]);
        }

        /// <summary>
        /// �w��ʒu���ӂ̋���擾
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