using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using System;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
public class VrMarker : MonoBehaviour
{
    enum Tool
    {
        Pen,
        Eraser,
        ColorPicker
    }

    // Public editables
    [SerializeField] private int _penSize = 5;
    [SerializeField] private Color _color;
    [SerializeField] bool _acceptMouseInput = false;
    [SerializeField] int _frameDelay = 2;
    // Be sure the panels are in order of PEN, ERASER, COLOR PICKER
    [SerializeField] GameObject[] _toolPanels;

    // Private Fields
    private Color[] _colors;
    private RaycastHit _touch;
    private Whiteboard _whiteboard;
    private Vector2 _touchPos, _lastTouchPos;
    private bool _touchedLastFrame;
    private Quaternion _lastTouchRot;
    private Plane _plane = new Plane(Vector3.up, 0);

    private int _eraserSize = 5;
    private Tool _currentTool = Tool.Pen;

    // Undo specific fields
    private CappedStack<WhiteboardState> _wbStateStack = new CappedStack<WhiteboardState>(10);

    // Testing shtuff
    private int _framesPassedSinceApply = 0;

    // Vr
    [Header("Vr")]
    public XRController rightHand; // Need to connect rays to drawing
    public XRController leftHand;

    public XRRayInteractor rightRay;
    public XRRayInteractor leftRay;
    void Start()
    {
        //_renderer = _tip.GetComponent<Renderer>();

        _colors = Enumerable.Repeat(_color, _penSize * _penSize).ToArray();
        //_sizeText.text = _penSize.ToString();
    }


    void Update()
    {
        Vector3 pos = new Vector3(0, 0, 0);
        ///ChangeColor(_colorInput.Color());

        // Pen size
        if (Keyboard.current.leftBracketKey.wasPressedThisFrame)
        {
            ChangePenSize(-1);
        }

        if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
        {
            ChangePenSize(1);
        }

        // Change Tool NOTE: Add mouse interaction with UI in future
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ChangeTool(Tool.Pen);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            ChangeTool(Tool.Eraser);
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            ChangeTool(Tool.ColorPicker);
        }
        if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            Undo();
        }
        if (!Keyboard.current.leftCtrlKey.isPressed && !EventSystem.current.IsPointerOverGameObject())
        {
            switch (_currentTool)
            {
                case Tool.Pen:
                    Draw(); break;
                case Tool.Eraser:
                    Erase(); break;
                case Tool.ColorPicker:
                    PickColor(); break;
            }
        }
    }

    private void ChangeColor(Color c)
    {
        _color = c;
        _colors = Enumerable.Repeat(_color, _penSize * _penSize).ToArray();
    }
    private void ChangePenSize(int change)
    {
        switch (_currentTool)
        {
            case Tool.Pen:
                _penSize += change;
                _colors = Enumerable.Repeat(_color, _penSize * _penSize).ToArray();

                if (_penSize < 1) { _penSize = 1; }

                break;
            case Tool.Eraser:
                _eraserSize += change;
                _colors = Enumerable.Repeat(_color, _eraserSize * _eraserSize).ToArray();

                if (_eraserSize < 1) { _eraserSize = 1; }


                break;
        }
    }
    private void ChangeTool(Tool t)
    {
        _currentTool = t;
        
    }
    // Pen
    private void Draw()
    {
        Vector3 pointerPosition;
        if (_acceptMouseInput)
        {
            pointerPosition = Mouse.current.position.ReadValue();
        }
        else
        {
            pointerPosition = Pen.current.position.ReadValue();
        }
        Vector3 worldPos = new Vector3(0, 0, 0);
        if (Pen.current.tip.isPressed || (_acceptMouseInput && Pointer.current.press.isPressed))
        {
            RaycastHit hitData;
            pointerPosition.z = 1;
            var ray = Camera.main.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out hitData, 1000))
            {
                worldPos = hitData.point;
            }

        }
        if (worldPos != Vector3.zero)
        {
            worldPos.z -= .1f;
            Debug.DrawRay(worldPos, Vector3.forward, Color.green, 100f);
        }

        if (Physics.Raycast(Camera.main.ScreenPointToRay(pointerPosition), out _touch) && worldPos != Vector3.zero)
        {
            //Debug.Log("Touched");

            if (_touch.transform.CompareTag("Whiteboard"))
            {
                if (_whiteboard == null)
                {
                    _whiteboard = _touch.transform.GetComponent<Whiteboard>();
                }

                _touchPos = new Vector2(_touch.textureCoord.x, _touch.textureCoord.y);

                var x = (int)(_touchPos.x * _whiteboard.textureSize.x - (_penSize / 2));
                var y = (int)(_touchPos.y * _whiteboard.textureSize.y - (_penSize / 2));
                //Debug.Log("Touch Pos: " + _touchPos.x + " " + _touchPos.y);
                if (y < 0 || y > _whiteboard.textureSize.y || x < 0 || x > _whiteboard.textureSize.x)
                {
                    //Debug.Log("AHHHHHHHH!: " + x + ", " + y);
                    x = (int)((_touchPos.x * _whiteboard.textureSize.x - (_penSize / 2)) % _whiteboard.textureSize.x);
                    y = (int)((_touchPos.y * _whiteboard.textureSize.y - (_penSize / 2)) % _whiteboard.textureSize.y);
                    //return;
                }
                //Debug.Log("AHHHHHHHH! But Not :3 : " + x + ", " + y);
                if (_touchedLastFrame)
                {
                    _whiteboard.drawTexture.SetPixels(x, y, _penSize, _penSize, _colors);
                    for (float f = 0.01f; f < 1.00f; f += .01f)
                    {
                        var lerpX = (int)Mathf.Lerp(_lastTouchPos.x, x, f);
                        var lerpY = (int)Mathf.Lerp(_lastTouchPos.y, y, f);

                        // Set pixels
                        //Debug.Log("Pixels Set: (lerp) " + lerpX + ", " + lerpY);
                        //Debug.Log("Pixels Set: " + _touchPos.x + ", " + _touchPos);
                        if (!(lerpX + _penSize >= _whiteboard.textureSize.x) && !(lerpX + _penSize >= _whiteboard.textureSize.y))
                        {
                            _whiteboard.drawTexture.SetPixels((int)lerpX, (int)lerpY, _penSize, _penSize, _colors);
                        }
                    }

                    if (_framesPassedSinceApply >= _frameDelay)
                    {
                        _whiteboard.drawTexture.Apply();
                        _framesPassedSinceApply = 0;
                    }
                    else
                    {
                        _framesPassedSinceApply++;
                    }
                }

                if (!_touchedLastFrame)
                {
                    var wbState = new WhiteboardState(_whiteboard, _whiteboard.drawTexture);
                    _wbStateStack.Push(wbState);
                }

                _lastTouchPos = new Vector2(x, y);
                _lastTouchRot = transform.rotation;
                _touchedLastFrame = true;
                return;
            }
        }

        if (_whiteboard != null)
        {
            _whiteboard.drawTexture.Apply();
            _framesPassedSinceApply = 0;
        }
        _whiteboard = null;
        _touchedLastFrame = false;
    }

    // Eraser
    private void Erase()
    {
        Vector3 pointerPosition;
        if (_acceptMouseInput)
        {
            pointerPosition = Mouse.current.position.ReadValue();
        }
        else
        {
            pointerPosition = Pen.current.position.ReadValue();
        }
        Vector3 worldPos = new Vector3(0, 0, 0);
        if (Pen.current.tip.isPressed || (_acceptMouseInput && Pointer.current.press.isPressed))
        {
            RaycastHit hitData;
            pointerPosition.z = 1;
            var ray = Camera.main.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out hitData, 1000))
            {
                worldPos = hitData.point;
            }

        }
        if (worldPos != Vector3.zero)
        {
            worldPos.z -= .1f;
            Debug.DrawRay(worldPos, Vector3.forward, Color.green, 100f);
        }

        if (Physics.Raycast(Camera.main.ScreenPointToRay(pointerPosition), out _touch) && worldPos != Vector3.zero)
        {

            if (_touch.transform.CompareTag("Whiteboard"))
            {
                if (_whiteboard == null)
                {
                    _whiteboard = _touch.transform.GetComponent<Whiteboard>();
                }

                _touchPos = new Vector2(_touch.textureCoord.x, _touch.textureCoord.y);

                var x = (int)(_touchPos.x * _whiteboard.textureSize.x - (_eraserSize / 2));
                var y = (int)(_touchPos.y * _whiteboard.textureSize.y - (_eraserSize / 2));

                if (y < 0 || y > _whiteboard.textureSize.y || x < 0 || x > _whiteboard.textureSize.x)
                {
                    x = (int)((_touchPos.x * _whiteboard.textureSize.x - (_penSize / 2)) % _whiteboard.textureSize.x);
                    y = (int)((_touchPos.y * _whiteboard.textureSize.y - (_penSize / 2)) % _whiteboard.textureSize.y);
                    //return;
                }

                if (_touchedLastFrame)
                {
                    Color[] blankColors = new Color[_eraserSize * _eraserSize];
                    //Array.Fill<Color>(blankColors, new Color(0, 0, 0, 0)); //.NET 2.1
                    FillArray<Color>(blankColors, new Color(0, 0, 0, 0)); //not .NET 2.1
                    _whiteboard.drawTexture.SetPixels(x, y, _eraserSize, _eraserSize, blankColors);

                    for (float f = 0.01f; f < 1.00f; f += 0.01f)
                    {
                        var lerpX = (int)Mathf.Lerp(_lastTouchPos.x, x, f);
                        var lerpY = (int)Mathf.Lerp(_lastTouchPos.y, y, f);

                        // Set pixels
                        _whiteboard.drawTexture.SetPixels(lerpX, lerpY, _eraserSize, _eraserSize, blankColors);
                    }


                    if (_framesPassedSinceApply >= _frameDelay)
                    {
                        _whiteboard.drawTexture.Apply();
                        _framesPassedSinceApply = 0;
                    }
                    else
                    {
                        _framesPassedSinceApply++;
                    }
                }

                if (!_touchedLastFrame)
                {
                    var wbState = new WhiteboardState(_whiteboard, _whiteboard.drawTexture);
                    _wbStateStack.Push(wbState);
                }

                _lastTouchPos = new Vector2(x, y);
                _lastTouchRot = transform.rotation;
                _touchedLastFrame = true;
                return;
            }
        }

        if (_whiteboard != null)
        {
            _whiteboard.drawTexture.Apply();
            _framesPassedSinceApply = 0;
        }
        _whiteboard = null;
        _touchedLastFrame = false;
    }

    // Color Picker NOTE: Needs some positional work
    private void PickColor()
    {
        Vector3 pointerPosition;
        if (_acceptMouseInput)
        {
            pointerPosition = Mouse.current.position.ReadValue();
        }
        else
        {
            pointerPosition = Pen.current.position.ReadValue();
        }
        Vector3 worldPos = new Vector3(0, 0, 0);
        if (Pen.current.tip.isPressed || (_acceptMouseInput && Pointer.current.press.isPressed))
        {
            RaycastHit hitData;
            pointerPosition.z = 1;
            var ray = Camera.main.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out hitData, 1000))
            {
                worldPos = hitData.point;
            }

        }
        if (worldPos != Vector3.zero)
        {
            worldPos.z -= .1f;
            Debug.DrawRay(worldPos, Vector3.forward, Color.green, 100f);
        }

        if (Physics.Raycast(Camera.main.ScreenPointToRay(pointerPosition), out _touch) && worldPos != Vector3.zero)
        {
            if (_touch.transform.CompareTag("Whiteboard"))
            {
                Whiteboard wb = _touch.transform.GetComponent<Whiteboard>();
                _touchPos = new Vector2(_touch.textureCoord.x, _touch.textureCoord.y);

                var x = (int)(_touchPos.x * wb.textureSize.x - (_penSize / 2));
                var y = (int)(_touchPos.y * wb.textureSize.y - (_penSize / 2));

                if (y < 0 || y > wb.textureSize.y || x < 0 || x > wb.textureSize.x)
                {
                    //Debug.Log("AHHHHHHHH!: " + x + ", " + y);
                    x = (int)((_touchPos.x * wb.textureSize.x - (_penSize / 2)) % wb.textureSize.x);
                    y = (int)((_touchPos.y * wb.textureSize.y - (_penSize / 2)) % wb.textureSize.y);
                    //return;
                }

                var color = wb.drawTexture.GetPixel(x, y);
                //Debug.Log(color.ToString());
                ChangeColor(color);
            }
        }
    }

    private void Undo()
    {
        var prevState = _wbStateStack.Pop();
        if (prevState == null)
        {
            Debug.Log("Twas Null");
            return;
        }

        Graphics.CopyTexture(prevState.texture, prevState.wb.drawTexture);
        prevState.wb.drawTexture.Apply();
        prevState = null;
    }

    /**
     * Functions for the RGB sliders
     */
    public void ChangeToPen()
    {
        ChangeTool(Tool.Pen);
    }

    public void ChangeToEraser()
    {
        ChangeTool(Tool.Eraser);
    }

    public void ChangeToColorPicker()
    {
        ChangeTool(Tool.ColorPicker);
    }

    private void FillArray<T>(T[] arr, T element)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = element;
        }
    }

    public void ChangeWhiteboardStackCap(float f)
    {
        ChangeWhiteboardStackCap((int)f);
    }
    public void ChangeWhiteboardStackCap(int n)
    {
        _wbStateStack.ChangeCap(n);
        Debug.Log("Stack changed to " + n);
    }

    private class WhiteboardState
    {
        public Whiteboard wb { get; }
        public Texture2D texture { get; }

        public WhiteboardState(Whiteboard whb)
        {
            wb = whb;
            texture = new Texture2D(whb.drawTexture.width, whb.drawTexture.height);
            Graphics.CopyTexture(whb.drawTexture, texture);
            texture.Apply();
        }

        public WhiteboardState(Whiteboard whb, Texture2D tex)
        {
            wb = whb;
            texture = new Texture2D(tex.width, tex.height);
            Graphics.CopyTexture(tex, texture);
            texture.Apply();
        }

    }


}