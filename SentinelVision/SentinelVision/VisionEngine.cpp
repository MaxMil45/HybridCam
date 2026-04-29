#include "pch.h"

#include "VisionEngine.h"
#include <opencv2/opencv.hpp>

cv::VideoCapture cap;

bool InitCamera() {
    cap.open(0);
    return cap.isOpened();
}

bool GetNextFrame(unsigned char* buffer, int width, int height) {
    if (!cap.isOpened()) return false;
    cv::Mat frame;
    cap >> frame;
    if (frame.empty()) return false;

    cv::Mat resizedFrame;
    cv::resize(frame, resizedFrame, cv::Size(width, height));

    size_t dataSize = width * height * 3;
    std::memcpy(buffer, resizedFrame.data, dataSize);
    return true;
}

void CloseCamera() {
    if (cap.isOpened()) cap.release();
}