"""
这段代码手写实现了一个 SVG 转 XAML 的 Python 脚本。
我研究了 SVG 是如何表示一段曲线的，想要通过这个曲线表示法找到曲线终点的位置，最后在 Google 上找到了
发布在 pip 上的库 svgpathtools，它可以提取 svg 文件中的 所有 path, circle, line 属性并将它们以
字典的形式（也就是json）存储在类中。
更重要的是，它还支持计算 path 的长度（使用 Path.length() 方法）。
因此此段脚本弃用，我将写新的脚本取代它。
"""

svgFile = open("SchoolMap.svg", "r")
file_string = svgFile.read()
svgFile.close()


def findPaths(fileStr):
    pathOutputList = []
    start = 0
    while True:
        # 找到 Path 元素的起始位置
        start = fileStr.find("<path", start)

        # 如果失败，直接退出
        if start == -1:
            break

        # 找到 d=
        start = fileStr.find('d="', start) + len('d="')
        # 找到结束位置
        end = fileStr.find('"', start)

        pathOutputList.append(fileStr[start:end])
        start = end + len('"') + 1

    # list[str]
    return pathOutputList


def findCircles(fileStr):
    circleOutputList = []
    start = 0
    locationStarter = fileStr.find('<g id="Location">', start)
    verticeStarter = fileStr.find('<g id="Vertice">', start)
    
    
    # circle 的坐标 (x, y) 和 半径 (r) 的值
    x = 0
    y = 0
    r = 0

    while True:
        # 找到 Circle 元素的起始位置
        start = fileStr.find("<circle", start)
        belongTo = 0 if start >= verticeStarter and start < locationStarter else 1
        # 如果失败，直接退出
        if start == -1:
            break

        # 找到 cx=
        start = fileStr.find('cx="', start) + len('cx="')
        # 找到结束位置
        end = fileStr.find('"', start)
        # 获取x值
        x = fileStr[start:end]

        # 找到 cy= 并获取y值
        start = fileStr.find('cy="', end) + len('cy="')
        end = fileStr.find('"', start)
        y = fileStr[start:end]

        # 找到 r= 并获取r值
        start = fileStr.find('r="', end) + len('r="')
        end = fileStr.find('"', start)
        r = fileStr[start:end]

        # 插入到列表的尾部
        circleOutputList.append((x, y, r, belongTo))
        start = end + len('"/>') + 1

    # list[(str, str, str, int)] 元组
    return circleOutputList


def getPaths(fileStr, method, **kwargs):
    result = []
    for path in findPaths(fileStr):
        result.append(method(path, **kwargs))

    # list[str]
    return result


def getCircles(fileStr, method, **kwargs):
    result = []
    for circle in findCircles(fileStr):
        result.append(method(circle[0], circle[1], circle[2], circle[3], **kwargs))

    # list[str]
    return result


def getXAMLPath(path, **kwargs):
    return f"""
<Path Stroke="{kwargs["strokeColor" if "strokeColor" in kwargs.keys() else "Black"]}"
      StrokeThickness="{kwargs["strokeThickness"] if "strokeThickness" in kwargs.keys() else "3"}">
    <Path.Data>
        <PathGeometry Figures="{path}" />
    </Path.Data>
</Path>"""

def getXAMLEllipse(x, y, r, belongTo, **kwargs):
    locationColor = kwargs["locationColor"] if "locationColor" in kwargs.keys() else ""
    verticeColor = kwargs["verticeColor"] if "verticeColor" in kwargs.keys() else ""
    # SVG 转 XAML 时需要将数值转化，只适用于无边框圆形
    return f"""
<Ellipse Fill="{locationColor if belongTo == 1 else verticeColor}"
         Canvas.Left="{float(x) - float(r)}"
         Canvas.Top="{float(y) - float(r)}"
         Height="{float(r) * 2}"
         Width="{float(r) * 2}" />"""


DEBUG = False
if DEBUG:
    for i in findPaths(file_string):
        print(i)
    print("-------------------")    
    for i in findCircles(file_string):
        print(i)
else:
    output = open("output.xaml", "w")
    output.writelines(getPaths(file_string, getXAMLPath, strokeColor="Green", strokeThickness="3"))
    output.writelines(getCircles(file_string, getXAMLEllipse, locationColor="Red", verticeColor="Black"))
    output.close()
