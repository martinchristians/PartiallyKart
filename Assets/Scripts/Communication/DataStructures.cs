using System.Collections.Generic;
using UnityEngine;

namespace Communication {

    [System.Serializable]
    struct GameMessage {

        [SerializeField] public string type;
        [SerializeField] public bool paused;
        [SerializeField] public string[] buttons;
        [SerializeField] public bool enabled;
        [SerializeField] public string layout;  // TODO object? because of gamepadlayout?
        
    }

    [System.Serializable]
    struct ServerOrClientMessage {

        [SerializeField] public string type;
        [SerializeField] public string room_code;
        [SerializeField] public int id;
        [SerializeField] public string name;
        [SerializeField] public string code;
        [SerializeField] public string message;
        [SerializeField] public PlayerData[] players;
        [SerializeField] public bool paused;
        [SerializeField] public int level;
        [SerializeField] public string button;
        [SerializeField] public bool pressed;

    }

    [System.Serializable]
    struct PlayerData {

        [SerializeField] public int id;
        [SerializeField] public string name;

        public override bool Equals (object obj) {
            if(obj is PlayerData otherPlayerData){
                return otherPlayerData.id == this.id;
            }
            return false;
        }

        public override int GetHashCode () {
            return id;
        }

    }

    static class Button {

        public const string left = "left";
        public const string right = "right";
        public const string forward = "go";
        public const string back = "stop";

        public static IEnumerable<string> all { get {
            yield return left;
            yield return right;
            yield return forward;
            yield return back;
        } }

    }

}