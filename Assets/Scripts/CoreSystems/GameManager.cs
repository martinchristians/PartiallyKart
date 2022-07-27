using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Communication;

namespace CoreSystems {

    public class GameManager : MonoBehaviour {

        const int MAIN_MENU_SCENE_INDEX = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateInstance () {
            DontDestroyOnLoad(new GameObject("[Game Manager]", typeof(GameManager)));
        }

        bool m_loading = false;
        int m_nextScene = -1;

        static List<PlayerData> m_currentLevelPlayers = new List<PlayerData>();
        static List<PlayerData> m_currentLevelSpectators = new List<PlayerData>();

        static bool isInLevel => SceneManager.GetActiveScene().buildIndex != 0;
        static GamepadLayout currentLayout => (SceneManager.GetActiveScene().buildIndex == 3) ? GamepadLayout.jump : GamepadLayout.standard;

        public static bool isPaused { get; private set; }
        public static PlayerData pausedByPlayerData { get; private set; }

        public static IReadOnlyList<PlayerData> players => m_currentLevelPlayers;
        public static IReadOnlyList<PlayerData> spectators => m_currentLevelSpectators;

        void Awake () {
            // SaveFile.ReadFromDisk();
            UI.Ingame.GameUI.EnsureExists();
            UI.PauseMenu.EnsureExists();
            SFX.EnsureExists();
            GameClient.onLevelStartRequested += OnLevelRequested;
            GameClient.onMainMenuRequested += OnMainMenuRequested;
            GameClient.onPlayerLeft += OnPlayerLeft;
            GameClient.onPlayerJoined += OnPlayerJoined;
            GameClient.onPauseRequested += OnPauseRequested;
            GameClient.onUnpauseRequested += OnUnPauseRequested;
            UI.Ingame.GameUI.instance.visible = isInLevel;
            if(isInLevel){
                SpawnCarAndResume();
            }

            void OnLevelRequested (int requestedLevel) {
                if(m_loading || isInLevel){
                    return;
                }
                m_nextScene = requestedLevel;
            }

            void OnMainMenuRequested (PlayerData mainMenuPlayer) {
                if(m_loading || !isInLevel){
                    return;
                }
                if(isPaused){
                    if(!mainMenuPlayer.Equals(pausedByPlayerData)){
                        return;
                    }
                }
                m_nextScene = MAIN_MENU_SCENE_INDEX;
            }

            void OnPlayerLeft (PlayerData leftPlayer) {
                if(m_loading || !isInLevel){
                    return;
                }
                m_currentLevelPlayers.Remove(leftPlayer);
                m_currentLevelSpectators.Remove(leftPlayer);
                if(m_currentLevelPlayers.Count < 1){
                    m_nextScene = MAIN_MENU_SCENE_INDEX;
                }else{
                    IngameButtonLayout.ApplyForPlayers(m_currentLevelPlayers);
                    if(isPaused && leftPlayer.Equals(pausedByPlayerData)){
                        Unpause();
                    }
                }
            }

            void OnPlayerJoined (PlayerData newPlayer) {
                if(m_loading || !isInLevel){
                    return;
                }
                m_currentLevelSpectators.Add(newPlayer);
                GameClient.SendLevelStarted(newPlayer.id, currentLayout);
                GameClient.SendButtonsEnabled(newPlayer.id, Button.all, false);
            }

            void OnPauseRequested (PlayerData pausePlayer) {
                if(!m_loading && isInLevel && !isPaused && IsAnActivePlayer(pausePlayer)){
                    Pause(pausePlayer);
                }
            }

            void OnUnPauseRequested (PlayerData unpausePlayer) {
                if(isPaused && pausedByPlayerData.Equals(unpausePlayer)){
                    Unpause();
                }
            }
        }

        void Update () {
            if(m_loading){
                return;
            }
            if(m_nextScene >= 0){
                StartCoroutine(LoadScene(m_nextScene));
                m_nextScene = -1;
            }
        }

        IEnumerator LoadScene (int sceneIndex) {
            if(m_loading){
                Debug.LogError($"Already loading, aborting call to load scene \"{sceneIndex}\"!");
                yield break;
            }
            var path = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
            if(string.IsNullOrWhiteSpace(path)){
                Debug.LogError($"Invalid scene index \"{sceneIndex}\"!");
                yield break;
            }
            m_loading = true;
            yield return SceneManager.LoadSceneAsync(sceneIndex);
            if(sceneIndex > MAIN_MENU_SCENE_INDEX){
                GameClient.ResetButtonsPressed();
                GameClient.SendLevelStarted(currentLayout);
                m_currentLevelPlayers.Clear();
                m_currentLevelSpectators.Clear();
                foreach(var player in GameClient.connectedPlayers){
                    if(m_currentLevelPlayers.Count < 4){
                        m_currentLevelPlayers.Add(player);
                    }else{
                        m_currentLevelSpectators.Add(player);
                    }
                }
                if(m_currentLevelPlayers.Count > 0){
                    IngameButtonLayout.ApplyForPlayers(m_currentLevelPlayers);
                    foreach(var player in m_currentLevelSpectators){
                        GameClient.SendButtonsEnabled(player.id, Button.all, false);
                    }
                }else{
                    m_nextScene = MAIN_MENU_SCENE_INDEX;
                }
                SpawnCarAndResume();
            }else{
                GameClient.SendMainMenuOpened();
            }
            m_loading = false;
        }

        void SpawnCarAndResume () {
            var spawn = CarSpawn.current;
            if(spawn == null){
                Debug.LogWarning("Car spawn instance not set, looking via GameObject.FindObjectOfType.");
                spawn = GameObject.FindObjectOfType<CarSpawn>();
                if(spawn == null){
                    Debug.LogError("No spawn point found, doing default spawn!");
                    spawn = new GameObject("Emergency Spawn").AddComponent<CarSpawn>();
                    spawn.transform.position = Vector3.up;
                    spawn.transform.rotation = Quaternion.identity;
                }
            }
            var car = spawn.SpawnCar();
            car.onDied += OnPlayerDeath;
            if(GameClient.connected){
                Unpause();
            }
            car.inputBlocked = true;
            UI.Ingame.GameUI.instance.countdown.DoCountdown(() => {
                car.inputBlocked = false;
                if(Level.current != null){
                    Level.current.StartTimer();
                }
            });
        }

        bool IsAnActivePlayer (PlayerData playerToCheck) {
            foreach(var currentPlayer in m_currentLevelPlayers){
                if(currentPlayer.id == playerToCheck.id){
                    return true;
                }
            }
            return false;
        }

        void Pause (PlayerData pausePlayer) {
            isPaused = true;
            pausedByPlayerData = pausePlayer;
            UI.PauseMenu.instance.Show(pausePlayer.name);
            GameClient.UpdatePauseState(pausePlayer.id, true);
            Time.timeScale = 0f;
        }

        void Unpause () {
            isPaused = false;
            pausedByPlayerData = default;
            UI.PauseMenu.instance.Hide();
            GameClient.UpdatePauseState(-1, false);
            Time.timeScale = 1f;
        }

        void OnPlayerDeath () {
            Debug.Log("TODO game over");
            // show the game over screen
            // tell the clients that the game is over
            // options (return to menu, retry)
            if(Level.current != null){
                var levelSaveData = Level.current.GetSaveData();
                SaveFile.SetLevelSaveData(levelSaveData);
                SaveFile.IncreaseCoinCounter(levelSaveData.coinsCollected);
                SaveFile.IncreaseTotalPlayTime(levelSaveData.playDuration);
            }
        }

    }

}