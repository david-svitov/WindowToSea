using UnityEngine;
using UI = UnityEngine.UI;

namespace MediaPipe.BlazeFace
{

    public sealed class CameraProcessor : MonoBehaviour
    {
        #region Editable attributes

        [SerializeField] Texture2D _image = null;
        [SerializeField] WebcamInput _webcam = null;
        [Space]
        [SerializeField, Range(0, 1)] float _threshold = 0.75f;
        [Space]
        [SerializeField] UI.RawImage _previewUI = null;
        [Space]
        [SerializeField] ResourceSet _resources = null;
        [SerializeField] Marker _markerPrefab = null;
        [SerializeField] Camera sceneCamera = null;
        
        #endregion

        #region Private members

        FaceDetector _detector;
        Marker[] _markers = new Marker[16];
        Quaternion _nextPosition = Quaternion.Euler(0, 0, 0);
        Vector2 _prevEyesCenter = new Vector2(0, 0);
        const float _moveSpeed = 4f;
        const float _gridSize = 0.01f;


        void RunDetector(Texture input)
        {
            // Face detection
            _detector.ProcessImage(input, _threshold);

            // Marker update
            var i = 0;
            float maxEyesDistance = -1;
            Vector2 eyesCenter = new Vector2();
            foreach (var detection in _detector.Detections)
            {
                if (i == _markers.Length) break;
                var marker = _markers[i++];
                marker.detection = detection;
                marker.gameObject.SetActive(true);

                // Looking for the biggest face
                float eyesDistance = Vector2.Distance(detection.leftEye, detection.rightEye);
                if (eyesDistance > maxEyesDistance)
                {
                    maxEyesDistance = eyesDistance;
                    eyesCenter = Vector3.Lerp(detection.leftEye, detection.rightEye, 0.5f);
                }
            }

            for (; i < _markers.Length; i++)
                _markers[i].gameObject.SetActive(false);

            // UI update
            _previewUI.texture = input;

            // Update scene camera position
            if (maxEyesDistance > 0 && Vector2.Distance(eyesCenter, _prevEyesCenter) > _gridSize) { 
                Vector2 cameraAngles = trancsformToCameraAngles(eyesCenter, maxEyesDistance);
                _nextPosition = Quaternion.Euler(cameraAngles.y, cameraAngles.x, 0);
                _prevEyesCenter = eyesCenter;
            }
            sceneCamera.transform.rotation = Quaternion.Slerp(sceneCamera.transform.rotation, _nextPosition, Time.deltaTime * _moveSpeed);
        }

        /// <summary>
		/// Transform face position to camera angles
		/// </summary>
		private Vector2 trancsformToCameraAngles(Vector2 eyesCenter, float eyesDistance)
        {
            Vector2 cameraAngles = new Vector2();

            const float scale = 1.0f; // Convert meters to 'pixels'
            const float oneMeterSize = 0.05f; // Face size on 1 meter distance
            const float centerX = 0.5f;
            const float centerY = 0.5f;


            // Face position in pixels
            float Z = (oneMeterSize / eyesDistance) * scale;

            float offset_y = Mathf.Abs(eyesCenter.y - centerY) + 0.0001f;
            float offset_x = Mathf.Abs(eyesCenter.x - centerX) + 0.0001f;

            float direction_x = Mathf.Sign(eyesCenter.x - centerX);
            float direction_y = Mathf.Sign(eyesCenter.y - centerY);

            cameraAngles.y = 90 - Mathf.Rad2Deg * Mathf.Atan(Z / offset_y);
            cameraAngles.x = 90 - Mathf.Rad2Deg * Mathf.Atan(Z / offset_x);

            cameraAngles.y *= direction_y;
            cameraAngles.x *= direction_x;

            return cameraAngles;
        }

        #endregion

        #region MonoBehaviour implementation

        void Start()
        {
            // Face detector initialization
            _detector = new FaceDetector(_resources);

            // Marker population
            for (var i = 0; i < _markers.Length; i++)
                _markers[i] = Instantiate(_markerPrefab, _previewUI.transform);

            // Static image test: Run the detector once.
            if (_image != null) RunDetector(_image);
        }

        void OnDestroy()
          => _detector?.Dispose();

        void Update()
        {
            // Webcam test: Run the detector every frame.
            if (_webcam != null) RunDetector(_webcam.Texture);
        }

        #endregion
    }

} // namespace MediaPipe.BlazeFace
