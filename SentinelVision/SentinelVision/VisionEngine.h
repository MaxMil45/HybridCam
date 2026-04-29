#pragma once

// The extern "C" block prevents C++ from mangling the names, 
// ensuring C# can find them by these exact names.
extern "C" {
    __declspec(dllexport) bool InitCamera();
    __declspec(dllexport) bool GetNextFrame(unsigned char* buffer, int width, int height);
    __declspec(dllexport) void CloseCamera();
}