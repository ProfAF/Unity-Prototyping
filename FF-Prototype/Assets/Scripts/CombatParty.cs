﻿using UnityEngine;
using System.Collections.Generic;
using FiniteStateMachine;
namespace Party
{ /// <summary>
  /// keeps track of all units in this group and manages them
  /// we will funnel all unit actions through this object before a notification
  /// is registered. We do this because i don't know
  /// </summary>
    enum State
    {
        INIT,
        START,
        ACTIVE,
        INACTIVE,
        TURN,
        EXIT,
    }

    public class CombatParty : Observer
    {

        public void Activate()
        {
            Debug.Log("activate party");
            UpdateFSM("activate");            
            _active = true;
        }

        void Awake()
        {
            fsm = new FiniteStateMachine<State>();
            fsm.State(State.INIT, InitHandler);
            fsm.State(State.START, StartHandler);
            fsm.State(State.INACTIVE, InactiveHandler);
            fsm.State(State.ACTIVE, ActiveHandler);
            fsm.State(State.TURN, TurnHandler);
            fsm.State(State.EXIT, ExitHandler);

            fsm.Transition(State.INIT, State.START, "start");
            fsm.Transition(State.START, State.INACTIVE, "inactive");
            fsm.Transition(State.INACTIVE, State.ACTIVE, "activate");
            fsm.Transition(State.ACTIVE, State.TURN, "endturn"); //on combat resolve go to party resolve

            fsm.Transition(State.TURN, State.ACTIVE, "active");            
            fsm.Transition(State.TURN, State.EXIT, "done");
            fsm.Transition(State.EXIT, State.INACTIVE, "inactive");
            UpdateFSM("*");
        }

        void OnStateChange(State state)
        {
            Debug.Log("State change: Party: " + state.ToString().ToLower());
            Publish(MessageLayer.PARTY, "state change", state.ToString().ToLower() );
        }

        void UpdateFSM(string input)
        {
            //Debug.Log("feed party fsm with " + input);            
            fsm.Feed(input);
        }

        void InitHandler()
        {
            OnStateChange(State.INIT);
            //subscribe to combat state changes and give fsm the result
            Subscribe<string>(MessageLayer.COMBAT, "enter state", UpdateFSM); 
            _partyMembers = ChuTools.PopulateFromChildren<CombatUnit>(transform);


        }

        void StartHandler() //start of party
        {
            OnStateChange(State.START);            
            _currentUnit = _partyMembers[_unitIndex];
            UpdateFSM("inactive");
        }

        void InactiveHandler() //start of unit
        {
            OnStateChange(State.INACTIVE);            
            
        }

        void ActiveHandler() //start of unit
        {
            OnStateChange(State.ACTIVE);
            _currentUnit = _partyMembers[_unitIndex];
            _currentUnit.SetActive(true);
        }

        void TurnHandler()
        {
            OnStateChange(State.TURN);
            if (_unitIndex >= _partyMembers.Count - 1)
            {
                _currentUnit.SetActive(false);
                _currentUnit = null;
                UpdateFSM("done");
                return;
            }

            _currentUnit.SetActive(false);
            _unitIndex++; //increment the unit index  
            turnsTaken++;
        }

        void ExitHandler()
        {
            OnStateChange(State.EXIT);
            EventSystem.RemoveSubscriber(MessageLayer.COMBAT, "state change", this);
            _unitIndex = 0;
            UpdateFSM("inactive");
        }



        #region Variables
        private FiniteStateMachine<State> fsm;
        /// <summary>
        /// the list of units participating in combat
        /// </summary>
        [SerializeField]
        private List<CombatUnit> _partyMembers;

        /// <summary>
        /// current index of the unit taking turn
        /// </summary>
        [SerializeField]
        private int _unitIndex;

        [SerializeField]
        private bool _active;


        /// <summary>
        /// the instance of the current unit taking turn
        /// </summary>
        [SerializeField]
        private CombatUnit _currentUnit;

        public CombatUnit CurrentUnit
        {
            get { return _currentUnit; }
        }
        /// <summary>
        /// how many turns we have taken
        /// </summary>
        public int turnsTaken;

        /// <summary>
        /// number of members in the group
        /// </summary>    
        public int partySize
        {
            get { return _partyMembers.Count; }
        }

        #endregion Variables
    }
}