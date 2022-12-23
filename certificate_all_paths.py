"""
您可以用这个脚本检测从 Adobe Illustrator 导出的SVG中，路径的起点和终点是否在已有的节点上。
如果有路径的起点或终点不在[节点]列表内，则会影响程序的运行。

如果没有任何问题，脚本会输出 "------------"
如果有遗漏的路径，脚本会输出起点/终点的坐标，以 Python 内置 complex() 类型的格式。

如：坐标为 (114, 514) 的节点表示为 (114+514j).

请带着这些节点返回 Adobe Illustrator 修复问题，重画路径/节点。
"""

from svgpathtools import svg2paths
import json

paths, attributes = svg2paths("SchoolMap.svg")

outputPaths = []
outputRelated = []
outputVertices = []
outputLocations = []

for i in range(len(paths)):
    if "r" not in attributes[i].keys():
        if "id" not in attributes[i].keys():
            outputPaths.append(paths[i])
    else:
        if attributes[i]["id"].startswith("_"):
            outputVertices.append(attributes[i])
        else:
            outputLocations.append(attributes[i])

locationCenter = []
for location in outputLocations:
    locationCenter.append(complex(float(location["cx"]), float(location["cy"])))

verticeCenter = []

for vertice in outputVertices:
    verticeCenter.append(complex(float(vertice["cx"]), float(vertice["cy"])))

verticeCenter += locationCenter

pathEnds = []

for path in outputPaths:
    pathEnds.append(path.start)
    pathEnds.append(path.end)


# relatedEnds = []

# for relatePath , name in outputRelated:
#     relatedEnds.append((relatePath.start, relatePath.end))


for i in pathEnds:
    if i in verticeCenter:
        continue
    print(i)

print("------------")

# for start, end in relatedEnds:
#     if start in verticeCenter and end in locationCenter:
#         continue
#     elif start in locationCenter and end in verticeCenter:
#         continue
#     print(start, end)

