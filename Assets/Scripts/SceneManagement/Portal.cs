using System.Collections;
using RPG.Control;
using RPG.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace RPG.SceneManagement
{
    public class Portal : MonoBehaviour
    {
        private enum DestinationIdentifier
        {
            A, B, C, D
        }

        [SerializeField] private int sceneToLoad = -1;
        [SerializeField] public Transform spawnPoint;
        [SerializeField] private DestinationIdentifier destination = DestinationIdentifier.A;
        [SerializeField] private float fadeOutTime = 0.5f;
        [SerializeField] private float fadeInTime = 1f;
        [SerializeField] private float fadeWaitTime = 0.5f;

        private void OnTriggerEnter(Collider other)
        {
            if (false == other.gameObject.CompareTag("Player")) return;

            // 注册一个协程
            StartCoroutine(Transition());
        }

        /**
         * 切换scene的函数
         * "协程系统"不断的循环调用这个函数，直至没有函数体为止
         * 但也有一个问题：拥有这个函数的gameObject因为切换场景而销毁时，这个协程也会停止, 英文原文
         * But there is somthing tha is going to stop this from happening
         * that's the fact that as soon as we load this new scene
         * at portal game object the one which has the coroutine gets destroyed and so does the coroutine itself
         *
         * 1、调用切换scene的函数
         * 2、调用切换场景之后的一些函数，比如保存一些数据
         */

        private IEnumerator Transition()
        {
            if (sceneToLoad < 0)
            {
                Debug.LogError("Scene to load not set.");
                yield break;
            }

            DontDestroyOnLoad(this.gameObject);

            Fader fade = GameObject.FindObjectOfType<Fader>();
            SavingWrapper wrapper = GameObject.FindObjectOfType<SavingWrapper>();
            var playerController = GameObject.FindObjectOfType<PlayerController>();
            playerController.enabled = false;

            yield return fade.FadeOut(fadeOutTime);

            wrapper.Save();

            yield return SceneManager.LoadSceneAsync(sceneToLoad);
            var newPlayerController = GameObject.FindObjectOfType<PlayerController>();
            newPlayerController.enabled = false;

            wrapper.Load();

            Portal otherPortal = GetOtherPortal();
            UpdatePlayer(otherPortal);

            // another save for reload scene
            wrapper.Save();

            yield return new WaitForSeconds(fadeWaitTime);
            fade.FadeIn(fadeInTime);

            newPlayerController.enabled = true;
            Destroy(this.gameObject);
        }

        private void UpdatePlayer(Portal otherPortal)
        {
            GameObject player = GameObject.FindWithTag("Player");
            // player.transform.position = otherPortal.spawnPoint.position;
            player.GetComponent<NavMeshAgent>().Warp(otherPortal.spawnPoint.position);
            player.transform.rotation = otherPortal.spawnPoint.rotation;
        }

        private Portal GetOtherPortal()
        {
            foreach (var portal in FindObjectsOfType<Portal>())
            {
                if (portal == this) continue;
                if (portal.destination != this.destination) continue;
                return portal;
            }
            return null;
        }
    }
}