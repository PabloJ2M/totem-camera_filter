using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Mediapipe.Unity.Sample
{
    public class DropdownCamera : MonoBehaviour
    {
        [SerializeField] private GameObject _behaviours;
        [SerializeField] private TMP_Dropdown _dropdown;

        private IEnumerator Start()
        {
            _dropdown.ClearOptions();
            _dropdown.onValueChanged.RemoveAllListeners();

            yield return new WaitUntil(() => ImageSourceProvider.ImageSource != null);
            var imageSource = ImageSourceProvider.ImageSource;
            var sourceNames = imageSource.sourceCandidateNames;

            if (sourceNames == null) {
                _dropdown.enabled = false;
                yield break;
            }

            var options = new List<string>(sourceNames);
            _dropdown.AddOptions(options);

            var currentSourceName = imageSource.sourceName;
            var defaultValue = options.FindIndex(option => option == currentSourceName);

            if (PlayerPrefs.HasKey("Camera")) { _dropdown.value = PlayerPrefs.GetInt("Camera"); }
            else if (defaultValue >= 0) _dropdown.value = defaultValue;

            imageSource.SelectSource(_dropdown.value);

            _dropdown.onValueChanged.AddListener(delegate {
                Switch(imageSource, _dropdown.value);
            });
        }
        private void Switch(ImageSource source, int index)
        {
            var behaviours = _behaviours.GetComponentsInChildren<BaseRunner>();

            foreach (var item in behaviours)
                item.Stop();

            source.SelectSource(index);
            PlayerPrefs.SetInt("Camera", index);

            foreach (var item in behaviours)
                item.Play();
        }
    }
}