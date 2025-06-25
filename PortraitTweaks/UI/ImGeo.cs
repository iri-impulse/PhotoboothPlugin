using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace PortraitTweaks.UI;

/// <summary>
/// Immediate-mode geometry utilities for ImGui, allowing for viewports with
/// their own coordinate systems and interactive drawings with draggable parts.
/// </summary>
/// <remarks>
/// This is a little overkill in some sense but there's a lot of prototyping to
/// get camera stuff feeling right and wherever possible we want to keep the
/// camera math and the canvas math as far apart as possible.
/// </remarks>
public static partial class ImGeo
{
    private static Viewport? _CurentView = null;

    private static Handle _CurrentHandle;

    private static uint _DeactivatedHandleId;
    private static uint _ActiveHandleId;

    public static ImDrawListPtr GetActiveDrawList()
    {
        return ImGui.GetWindowDrawList();
    }

    /// <summary>
    /// A RAII wrapper for an active canvas viewport.
    /// </summary>
    public class Canvas : IDisposable
    {
        public Canvas(string label, Vector2 min_xy, Vector2 max_xy, Vector2 size)
        {
            BeginCanvas(label, min_xy, max_xy, size);
        }

        public void Dispose()
        {
            EndCanvas();
        }
    }

    public static void BeginCanvas(string label, Vector2 min_xy, Vector2 max_xy, Vector2 size)
    {
        ImGui.InvisibleButton(label, size, ImGuiButtonFlags.MouseButtonLeft);
        var top_left = ImGui.GetItemRectMin();
        var screen_size = ImGui.GetItemRectSize();

        _CurentView = new Viewport
        {
            ViewRectMin = min_xy,
            ViewRectSize = max_xy - min_xy,
            ScreenRectMin = top_left,
            ScreenRectSize = screen_size,
        };
        GetActiveDrawList().PushClipRect(top_left, top_left + screen_size, true);
        ImGui.PushID(label);

        if (ImGui.IsItemDeactivated())
        {
            _DeactivatedHandleId = _ActiveHandleId;
            _ActiveHandleId = 0;
        }
        else
        {
            _DeactivatedHandleId = 0;
        }
    }

    public static void EndCanvas()
    {
        ImGui.PopID();
        ImGui.PopClipRect();
        _CurentView = null;
        _CurrentHandle = new Handle { Shape = Handle.HandleShape.None };
        _DeactivatedHandleId = 0;
    }

    public static bool IsMouseInView()
    {
        if (_CurentView == null)
            return false;

        var mouse_pos = ImGui.GetMousePos();
        return mouse_pos.X >= _CurentView.ScreenRectMin.X
            && mouse_pos.Y >= _CurentView.ScreenRectMin.Y
            && mouse_pos.X <= _CurentView.ScreenRectMax.X
            && mouse_pos.Y <= _CurentView.ScreenRectMax.Y;
    }

    public static Vector2 MouseViewPos()
    {
        if (_CurentView == null)
            throw new InvalidOperationException("No viewport is currently active.");

        var mouse_pos = ImGui.GetMousePos();
        return _CurentView.ScreenToView(mouse_pos);
    }

    // Coordinate transformations.

    public static Vector2 ScreenToView(Vector2 screen)
    {
        if (_CurentView == null)
            throw new InvalidOperationException("No viewport is currently active.");
        return _CurentView.ScreenToView(screen);
    }

    public static Vector2 ViewToScreen(Vector2 view)
    {
        if (_CurentView == null)
            throw new InvalidOperationException("No viewport is currently active.");
        return _CurentView.ViewToScreen(view);
    }

    public static Vector2 ScaleToView(Vector2 screen)
    {
        if (_CurentView == null)
            throw new InvalidOperationException("No viewport is currently active.");

        return _CurentView.ScaleToView(screen);
    }

    public static Vector2 ScaleToScreen(Vector2 view)
    {
        if (_CurentView == null)
            throw new InvalidOperationException("No viewport is currently active.");
        return _CurentView.ScaleToScreen(view);
    }

    public static Vector2 GetPixelSize()
    {
        if (_CurentView == null)
            throw new InvalidOperationException("No viewport is currently active.");
        return _CurentView.ScaleToView(Vector2.One) * ImGuiHelpers.GlobalScale;
    }

    //
    // Draggable handles, which work similar to ImGui items or 2D sliders.
    //

    // General handle inspection functions, by analogy with ImGui `IsItemXYZ`.
    public static bool IsHandleHovered(ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        return _ActiveHandleId == 0
            && ImGui.IsItemHovered(flags)
            && _CurrentHandle.HitTest(MouseViewPos());
    }

    public static bool IsHandleClicked(ImGuiMouseButton button = ImGuiMouseButton.Left)
    {
        return ImGui.IsItemClicked(button) && _CurrentHandle.HitTest(MouseViewPos());
    }

    public static bool IsHandleActive()
    {
        return ImGui.IsItemActive() && _ActiveHandleId == _CurrentHandle.Id;
    }

    public static bool IsHandleActivated()
    {
        return ImGui.IsItemActivated() && _ActiveHandleId == _CurrentHandle.Id;
    }

    public static bool IsHandleDeactivated()
    {
        return ImGui.IsItemDeactivated() && _DeactivatedHandleId == _CurrentHandle.Id;
    }

    // Creating handles.
    private static bool DragHandle(Handle handle, ref Vector2 position, uint col = 0xFFFFFFFF)
    {
        // Set current/active handle info.
        _CurrentHandle = handle;
        var hovered = IsHandleHovered();
        if (hovered && ImGui.IsItemActivated())
        {
            _ActiveHandleId = handle.Id;
        }

        var opacity = (byte)((col & 0xFF000000) >> 0x18);
        var color = col & 0x00FFFFFF;
        var activeCol = color | ((opacity / 2u) << 0x18);
        var hoverCol = color | ((opacity / 3u) << 0x18);

        var active = IsHandleActive();
        if (active)
        {
            handle.SetCursor();
            handle.DrawFill(activeCol);
        }
        else if (hovered)
        {
            handle.SetCursor();
            handle.DrawFill(hoverCol);
        }
        handle.Draw(col);

        if (IsHandleActive())
        {
            var mouse = MouseViewPos();
            var drag = mouse - handle.Position;
            if (drag.LengthSquared() > 1e-6)
            {
                position = mouse;
                return true;
            }
        }

        return false;
    }

    public static bool DragHandleCircle(
        string label,
        ref Vector2 position,
        float size,
        uint col = 0xFFFFFFFF,
        ImGuiMouseCursor cursor = ImGuiMouseCursor.Hand
    )
    {
        var handle = new Handle(label)
        {
            Shape = Handle.HandleShape.Circle,
            Position = position,
            Size = size,
            Cursor = cursor,
        };

        return DragHandle(handle, ref position, col);
    }

    // ImGui drawing functions, scaled and clipped to the viewport.

    // Don't hesitate to wrap more if we need them (Copilot oneshots them,
    // with the C declarations in-context.)

    public static Vector2 CalcTextSize(string text, bool hideTextAfterDoubleHash = false)
    {
        return ScaleToView(ImGui.CalcTextSize(text, hideTextAfterDoubleHash));
    }

    // csharpier-ignore-start

    /*
    IMGUI_API void  AddLine(const ImVec2& p1, const ImVec2& p2, ImU32 col, float thickness = 1.0f);
    IMGUI_API void  AddRect(const ImVec2& p_min, const ImVec2& p_max, ImU32 col, float rounding = 0.0f, ImDrawFlags flags = 0, float thickness = 1.0f);   // a: upper-left, b: lower-right (== upper-left + size)
    IMGUI_API void  AddRectFilled(const ImVec2& p_min, const ImVec2& p_max, ImU32 col, float rounding = 0.0f, ImDrawFlags flags = 0);                     // a: upper-left, b: lower-right (== upper-left + size)
    IMGUI_API void  AddRectFilledMultiColor(const ImVec2& p_min, const ImVec2& p_max, ImU32 col_upr_left, ImU32 col_upr_right, ImU32 col_bot_right, ImU32 col_bot_left);
    IMGUI_API void  AddQuad(const ImVec2& p1, const ImVec2& p2, const ImVec2& p3, const ImVec2& p4, ImU32 col, float thickness = 1.0f);
    IMGUI_API void  AddQuadFilled(const ImVec2& p1, const ImVec2& p2, const ImVec2& p3, const ImVec2& p4, ImU32 col);
    IMGUI_API void  AddTriangle(const ImVec2& p1, const ImVec2& p2, const ImVec2& p3, ImU32 col, float thickness = 1.0f);
    IMGUI_API void  AddTriangleFilled(const ImVec2& p1, const ImVec2& p2, const ImVec2& p3, ImU32 col);
    IMGUI_API void  AddCircle(const ImVec2& center, float radius, ImU32 col, int num_segments = 0, float thickness = 1.0f);
    IMGUI_API void  AddCircleFilled(const ImVec2& center, float radius, ImU32 col, int num_segments = 0);
    IMGUI_API void  AddNgon(const ImVec2& center, float radius, ImU32 col, int num_segments, float thickness = 1.0f);
    IMGUI_API void  AddNgonFilled(const ImVec2& center, float radius, ImU32 col, int num_segments);
    IMGUI_API void  AddEllipse(const ImVec2& center, const ImVec2& radius, ImU32 col, float rot = 0.0f, int num_segments = 0, float thickness = 1.0f);
    IMGUI_API void  AddEllipseFilled(const ImVec2& center, const ImVec2& radius, ImU32 col, float rot = 0.0f, int num_segments = 0);
    IMGUI_API void  AddText(const ImVec2& pos, ImU32 col, const char* text_begin, const char* text_end = NULL);
    IMGUI_API void  AddText(ImFont* font, float font_size, const ImVec2& pos, ImU32 col, const char* text_begin, const char* text_end = NULL, float wrap_width = 0.0f, const ImVec4* cpu_fine_clip_rect = NULL);
    IMGUI_API void  AddBezierCubic(const ImVec2& p1, const ImVec2& p2, const ImVec2& p3, const ImVec2& p4, ImU32 col, float thickness, int num_segments = 0); // Cubic Bezier (4 control points)
    IMGUI_API void  AddBezierQuadratic(const ImVec2& p1, const ImVec2& p2, const ImVec2& p3, ImU32 col, float thickness, int num_segments = 0);               // Quadratic Bezier (3 control points)
    */

    public static void AddLine(Vector2 p1, Vector2 p2, uint col, float thickness = 1.0f)
    {
        thickness *= ImGuiHelpers.GlobalScale;
        GetActiveDrawList().AddLine(ViewToScreen(p1), ViewToScreen(p2), col, thickness);
    }

    public static void AddRect(Vector2 p_min, Vector2 p_max, uint col, float rounding = 0.0f, ImDrawFlags flags = 0, float thickness = 1.0f)
    {
        thickness *= ImGuiHelpers.GlobalScale;
        GetActiveDrawList().AddRect(ViewToScreen(p_min), ViewToScreen(p_max), col, rounding, flags, thickness);
    }

    public static void AddRectFilled(Vector2 p_min, Vector2 p_max, uint col, float rounding = 0.0f, ImDrawFlags flags = 0)
    {
        GetActiveDrawList().AddRectFilled(ViewToScreen(p_min), ViewToScreen(p_max), col, rounding, flags);
    }

    public static void AddTriangle(Vector2 p1, Vector2 p2, Vector2 p3, uint col, float thickness = 1.0f)
    {
        thickness *= ImGuiHelpers.GlobalScale;
        GetActiveDrawList().AddTriangle(ViewToScreen(p1), ViewToScreen(p2), ViewToScreen(p3), col, thickness);
    }

    public static void AddTriangleFilled(Vector2 p1, Vector2 p2, Vector2 p3, uint col)
    {
        GetActiveDrawList().AddTriangleFilled(ViewToScreen(p1), ViewToScreen(p2), ViewToScreen(p3), col);
    }

    public static void AddCircle(Vector2 center, float radius, uint col, int num_segments = 0, float thickness = 1.0f)
    {
        thickness *= ImGuiHelpers.GlobalScale;
        GetActiveDrawList().AddCircle(ViewToScreen(center), radius * ScaleToScreen(Vector2.One).X, col, num_segments, thickness);
    }

    public static void AddCircleFilled(Vector2 center, float radius, uint col, int num_segments = 0)
    {
        GetActiveDrawList().AddCircleFilled(ViewToScreen(center), radius * ScaleToScreen(Vector2.One).X, col, num_segments);
    }

    public static void AddText(Vector2 pos, uint col, string text)
    {
        GetActiveDrawList().AddText(ViewToScreen(pos), col, text);
    }

    public static void AddText(ImFontPtr font, float font_size, Vector2 pos, uint col, string text)
    {
        GetActiveDrawList().AddText(font, font_size, ViewToScreen(pos), col, text);
    }

    /*
    IMGUI_API void  AddPolyline(const ImVec2* points, int num_points, ImU32 col, ImDrawFlags flags, float thickness);
    IMGUI_API void  AddConvexPolyFilled(const ImVec2* points, int num_points, ImU32 col);
    IMGUI_API void  AddConcavePolyFilled(const ImVec2* points, int num_points, ImU32 col);
    */

    /*
    inline    void  PathClear()                                                 { _Path.Size = 0; }
    inline    void  PathLineTo(const ImVec2& pos)                               { _Path.push_back(pos); }
    inline    void  PathLineToMergeDuplicate(const ImVec2& pos)                 { if (_Path.Size == 0 || memcmp(&_Path.Data[_Path.Size - 1], &pos, 8) != 0) _Path.push_back(pos); }
    inline    void  PathFillConvex(ImU32 col)                                   { AddConvexPolyFilled(_Path.Data, _Path.Size, col); _Path.Size = 0; }
    inline    void  PathFillConcave(ImU32 col)                                  { AddConcavePolyFilled(_Path.Data, _Path.Size, col); _Path.Size = 0; }
    inline    void  PathStroke(ImU32 col, ImDrawFlags flags = 0, float thickness = 1.0f) { AddPolyline(_Path.Data, _Path.Size, col, flags, thickness); _Path.Size = 0; }
    IMGUI_API void  PathArcTo(const ImVec2& center, float radius, float a_min, float a_max, int num_segments = 0);
    IMGUI_API void  PathArcToFast(const ImVec2& center, float radius, int a_min_of_12, int a_max_of_12);                // Use precomputed angles for a 12 steps circle
    IMGUI_API void  PathEllipticalArcTo(const ImVec2& center, const ImVec2& radius, float rot, float a_min, float a_max, int num_segments = 0); // Ellipse
    IMGUI_API void  PathBezierCubicCurveTo(const ImVec2& p2, const ImVec2& p3, const ImVec2& p4, int num_segments = 0); // Cubic Bezier (4 control points)
    IMGUI_API void  PathBezierQuadraticCurveTo(const ImVec2& p2, const ImVec2& p3, int num_segments = 0);               // Quadratic Bezier (3 control points)
    IMGUI_API void  PathRect(const ImVec2& rect_min, const ImVec2& rect_max, float rounding = 0.0f, ImDrawFlags flags = 0);
    */

    public static void PathClear()
    {
        GetActiveDrawList().PathClear();
    }

    public static void PathLineTo(Vector2 pos)
    {
        GetActiveDrawList().PathLineTo(ViewToScreen(pos));
    }

    public static void PathLineToMergeDuplicate(Vector2 pos)
    {
        GetActiveDrawList().PathLineToMergeDuplicate(ViewToScreen(pos));
    }

    public static void PathFillConvex(uint col)
    {
        GetActiveDrawList().PathFillConvex(col);
    }

    public static void PathStroke(uint col, ImDrawFlags flags = 0, float thickness = 1.0f)
    {
        GetActiveDrawList().PathStroke(col, flags, thickness);
    }

    public static void PathArcTo(Vector2 center, float radius, float a_min, float a_max, int num_segments = 0)
    {
        GetActiveDrawList().PathArcTo(ViewToScreen(center), radius * ScaleToScreen(Vector2.One).X, a_min, a_max, num_segments);
    }

    public static void PathBezierCubicCurveTo(Vector2 p2, Vector2 p3, Vector2 p4, int num_segments = 0)
    {
        GetActiveDrawList().PathBezierCubicCurveTo(ViewToScreen(p2), ViewToScreen(p3), ViewToScreen(p4), num_segments);
    }

    // csharpier-ignore-end
}

internal class Viewport
{
    public Vector2 ViewRectMin { get; internal set; }
    public Vector2 ViewRectSize { get; internal set; }
    public Vector2 ScreenRectMin { get; internal set; }
    public Vector2 ScreenRectSize { get; internal set; }

    public Vector2 ViewRectMax
    {
        get { return ViewRectMin + ViewRectSize; }
    }

    public Vector2 ScreenRectMax
    {
        get { return ScreenRectMin + ScreenRectSize; }
    }

    public Vector2 ScreenToView(Vector2 screen)
    {
        return new Vector2(
            (screen.X - ScreenRectMin.X) / ScreenRectSize.X * ViewRectSize.X + ViewRectMin.X,
            (screen.Y - ScreenRectMin.Y) / ScreenRectSize.Y * ViewRectSize.Y + ViewRectMin.Y
        );
    }

    public Vector2 ViewToScreen(Vector2 view)
    {
        return new Vector2(
            (view.X - ViewRectMin.X) / ViewRectSize.X * ScreenRectSize.X + ScreenRectMin.X,
            (view.Y - ViewRectMin.Y) / ViewRectSize.Y * ScreenRectSize.Y + ScreenRectMin.Y
        );
    }

    public Vector2 ScaleToView(Vector2 screen)
    {
        return new Vector2(
            screen.X / ScreenRectSize.X * ViewRectSize.X,
            screen.Y / ScreenRectSize.Y * ViewRectSize.Y
        );
    }

    public Vector2 ScaleToScreen(Vector2 view)
    {
        return new Vector2(
            view.X / ViewRectSize.X * ScreenRectSize.X,
            view.Y / ViewRectSize.Y * ScreenRectSize.Y
        );
    }
}

internal struct Handle
{
    internal enum HandleShape
    {
        None,
        Circle,
        Square,
    }

    public uint Id { get; private set; }

    public HandleShape Shape { get; set; }

    public Vector2 Position { get; set; }
    public float Size { get; set; }

    public ImGuiMouseCursor Cursor { get; set; }

    public Handle(string label)
    {
        Id = ImGui.GetID(label);
    }

    public bool HitTest(Vector2 mousePos)
    {
        switch (Shape)
        {
            case HandleShape.None:
                return false;
            case HandleShape.Circle:
                return Vector2.DistanceSquared(Position, mousePos) <= Size * Size;
            case HandleShape.Square:
                var diff = Vector2.Abs(mousePos - Position);
                return diff.X <= Size && diff.Y <= Size;
            default:
                throw new InvalidOperationException();
        }
    }

    public void SetCursor()
    {
        if (Shape == HandleShape.None)
            return;
        ImGui.SetMouseCursor(Cursor);
    }

    public void DrawFill(uint color)
    {
        switch (Shape)
        {
            case HandleShape.None:
                return;
            case HandleShape.Circle:
                ImGeo.AddCircleFilled(Position, Size, color);
                break;
            case HandleShape.Square:
                ImGeo.AddRectFilled(
                    Position - new Vector2(Size, Size),
                    Position + new Vector2(Size, Size),
                    color
                );
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    public void Draw(uint color)
    {
        switch (Shape)
        {
            case HandleShape.None:
                return;
            case HandleShape.Circle:
                ImGeo.AddCircle(Position, Size, color);
                break;
            case HandleShape.Square:
                ImGeo.AddRect(
                    Position - new Vector2(Size, Size),
                    Position + new Vector2(Size, Size),
                    color
                );
                break;
            default:
                throw new InvalidOperationException();
        }
    }
}
