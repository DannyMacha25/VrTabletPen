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
    [SerializeField] private RGBInput _colorInput;
    [SerializeField] bool _acceptMouseInput = false;
    [SerializeField] TextMeshProUGUI _sizeText;
    [SerializeField] int _frameDelay = 2;
    // Be sure the panels are in order of PEN, ERASER, COLOR PICKER
    [SerializeField] GameObject[] _toolPanels;


    // Colors for UI
    private Color _toolSelectedColor = new Color(248 / 255f, 1f, 117 / 255f, 1f);
    private Color _toolUnSelectedColor = new Color(1f, 1f, 1f, 1f);

    // Private Fields
    private Color[] _colors;
    private bool _disableFirstFrame = true;
    private bool _passedFirstFrame = false;
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

    public XRRayInteractor rightRay;
    public XRRayInteractor leftRay;
    public XRController rightHand;

    public InputHelpers.Button undoButton;
    bool undoAlreadyPressed = false;
    // Testing shtuff
    private int _framesPassedSinceApply = 0;
    void Start()
    {
        //_renderer = _tip.GetComponent<Renderer>();

        _colors = Enumerable.Repeat(_color, _penSize * _penSize).ToArray();
        _sizeText.text = _penSize.ToString();
    }


    void Update()
    {
        ChangeColor(_colorInput.Color());
       

        if (UndoPressedThisFrame())
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
        switch(_currentTool)
        {
            case Tool.Pen:
                _penSize += change;
                _colors = Enumerable.Repeat(_color, _penSize * _penSize).ToArray();

                if (_penSize < 1) { _penSize = 1; }

                _sizeText.text = _penSize.ToString();
                break;
            case Tool.Eraser:
                _eraserSize += change;
                _colors = Enumerable.Repeat(_color, _eraserSize * _eraserSize).ToArray();

                if (_eraserSize < 1) { _eraserSize = 1; }

                _sizeText.text = _eraserSize.ToString();
                break;
        }
    }
    private void ChangeTool(Tool t)
    {
        _currentTool = t;
        switch(t)
        {
            case Tool.Pen:
                _toolPanels[0].GetComponent<UnityEngine.UI.Image>().color = _toolSelectedColor;
                _toolPanels[1].GetComponent<UnityEngine.UI.Image>().color = _toolUnSelectedColor;
                _toolPanels[2].GetComponent<UnityEngine.UI.Image>().color = _toolUnSelectedColor;
                _sizeText.text = _penSize.ToString();
                break;
            case Tool.Eraser:
                _toolPanels[0].GetComponent<UnityEngine.UI.Image>().color = _toolUnSelectedColor;
                _toolPanels[1].GetComponent<UnityEngine.UI.Image>().color = _toolSelectedColor;
                _toolPanels[2].GetComponent<UnityEngine.UI.Image>().color = _toolUnSelectedColor;
                _sizeText.text = _eraserSize.ToString();
                break;
            case Tool.ColorPicker:
                _toolPanels[0].GetComponent<UnityEngine.UI.Image>().color = _toolUnSelectedColor;
                _toolPanels[1].GetComponent<UnityEngine.UI.Image>().color = _toolUnSelectedColor;
                _toolPanels[2].GetComponent<UnityEngine.UI.Image>().color = _toolSelectedColor;
                _sizeText.text = " ";
                break;
        }
    }
    // Pen
    private void Draw() // Need to replicate erase like draw
    {
        if ((rightRay.TryGetCurrent3DRaycastHit(out _touch) && rightRay.isActiveAndEnabled) || (leftRay.TryGetCurrent3DRaycastHit(out _touch) && leftRay.isActiveAndEnabled))
        {
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
                Debug.Log("AHHHHHHHH! But Not :3 : " + x + ", " + y);
                if (_touchedLastFrame)
                {
                    Debug.Log("DAHHHHHHHH! But Not :3 : " + x + ", " + y);
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

                    if(_framesPassedSinceApply >= _frameDelay)
                    {
                        _whiteboard.drawTexture.Apply();
                        _framesPassedSinceApply = 0;
                    }else
                    {
                        _framesPassedSinceApply++;
                    } 
                }

                if(!_touchedLastFrame)
                {
                    var wbState = new WhiteboardState(_whiteboard, _whiteboard.drawTexture);
                    _wbStateStack.Push(wbState);
                }

                if (_disableFirstFrame && _passedFirstFrame)
                {
                    _lastTouchPos = new Vector2(x, y);
                    _lastTouchRot = transform.rotation;
                    _touchedLastFrame = true;
                } else if (_disableFirstFrame && !_passedFirstFrame)
                {
                    _passedFirstFrame = true;
                }
                
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
        _passedFirstFrame = false;
        _touch = new RaycastHit();
    }

    // Eraser
    private void Erase()
    {
 
        if ((rightRay.TryGetCurrent3DRaycastHit(out _touch) && rightRay.isActiveAndEnabled) || (leftRay.TryGetCurrent3DRaycastHit(out _touch) && leftRay.isActiveAndEnabled))
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

                if (_disableFirstFrame && _passedFirstFrame)
                {
                    _lastTouchPos = new Vector2(x, y);
                    _lastTouchRot = transform.rotation;
                    _touchedLastFrame = true;
                }
                else if (_disableFirstFrame && !_passedFirstFrame)
                {
                    _passedFirstFrame = true;
                }

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
        _passedFirstFrame = false;
        _touch = new RaycastHit();
    }

    // Color Picker NOTE: Needs some positional work
    private void PickColor()
    {
        if ((rightRay.TryGetCurrent3DRaycastHit(out _touch) && rightRay.isActiveAndEnabled) || (leftRay.TryGetCurrent3DRaycastHit(out _touch) && leftRay.isActiveAndEnabled))
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
                _colorInput.UpdateColor(color);
            }
        }
    }

    public void LowerPenSize()
    {
        switch (_currentTool)
        {
            case Tool.Pen:
                _penSize -= 1;
                _colors = Enumerable.Repeat(_color, _penSize * _penSize).ToArray();

                if (_penSize < 1) { _penSize = 1; }

                _sizeText.text = _penSize.ToString();
                break;
            case Tool.Eraser:
                _eraserSize -= 1;
                _colors = Enumerable.Repeat(_color, _eraserSize * _eraserSize).ToArray();

                if (_eraserSize < 1) { _eraserSize = 1; }

                _sizeText.text = _eraserSize.ToString();
                break;
        }
    }

    public void IncreasePenSize()
    {
        switch (_currentTool)
        {
            case Tool.Pen:
                _penSize += 1;
                _colors = Enumerable.Repeat(_color, _penSize * _penSize).ToArray();

                if (_penSize < 1) { _penSize = 1; }

                _sizeText.text = _penSize.ToString();
                break;
            case Tool.Eraser:
                _eraserSize += 1;
                _colors = Enumerable.Repeat(_color, _eraserSize * _eraserSize).ToArray();

                if (_eraserSize < 1) { _eraserSize = 1; }

                _sizeText.text = _eraserSize.ToString();
                break;
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

    bool UndoPressedThisFrame()
    {
        bool pressed = false;
        rightHand.inputDevice.IsPressed(undoButton, out pressed);
        if(!pressed)
        {
            undoAlreadyPressed = false;
            return false;
        }
        if(undoAlreadyPressed)
        {
            return false;
        } else
        {
            undoAlreadyPressed = true;
            return true;
        }
    }

    /**
     * Functions for the RGB sliders
     */
    public void ChangeToPen()
    {
        Debug.Log("called");
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
        for(int i = 0; i < arr.Length; i++)
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