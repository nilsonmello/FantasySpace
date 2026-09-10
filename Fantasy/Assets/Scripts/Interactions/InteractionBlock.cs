using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class SimpleInteractable : InteractionManager
{

            [Serializable]
        /// <summary>
        /// Function definition for a button click event.
        /// </summary>
        public class ButtonClickedEvent : UnityEvent {}

        // Event delegates triggered on click.
        [FormerlySerializedAs("onInteract")]
        [SerializeField]
        private ButtonClickedEvent m_OnClick = new ButtonClickedEvent();

    public override void interact()
    {
        m_OnClick.Invoke();
    }
}