export function drawGraph(json, forcesConfig) {
  // remove all elements in `#network-area` div
  // copy svg with id `#network-template` with id removed
  // add to `#network-area` div

  const svgTemplate = document.querySelector("#network-template");
  const svgCopy = svgTemplate.cloneNode(true);
  svgCopy.removeAttribute("id");
  svgCopy.id = "svg";
  const networkArea = document.querySelector("#network-area");
  networkArea.innerHTML = "";
  networkArea.appendChild(svgCopy);

  const width = networkArea.clientWidth;
  const height = networkArea.clientHeight;

  // const color = d3.scaleOrdinal(d3.schemeCategory20);
  const color = (group) => {
    switch (group) {
      case 0:
        return "#1f77b4";
      case 1:
        return "#aec7e8";
      case 2:
        return "#ff7f0e";
      default:
        return "#666";
    }
  }

  const radius = function (group) {
    switch (group) {
      case 0:
        return 25;
      case 1:
        return 15;
      case 2:
        return 30;
      default:
        return 6;
    }
  };

  const svg = d3.select("#svg")
    .attr("width", width)
    .attr("height", height);

  let simulation = d3
    .forceSimulation()
    .force("link", d3
      .forceLink()
      .id(function (d) {
        return d.id;
      })
      .distance(function (d) {
        // const baseDist = radius(d.source.group) + radius(d.target.group) + 10;
        // const minDist = 100; // specify minimum link length
        // return Math.max(minDist, baseDist);
        return 100;
      })
    )
    // .force("charge", d3.forceManyBody()
    //   .strength(function (d) {
    //     return d.fixed ? 0 : -170;
    //   })
    // )
    // .force("center", d3.forceCenter(width / 2, height / 2))

    .force("collision", d3.forceCollide()
      .radius(function (d) {
        return d.fixed ? 0 : radius(d.group) + 5;
      })
    )
  // .force("x", d3.forceX().strength(0.015))
  // .force("y", d3.forceY().strength(0.015));

  // setTimeout(() => {
  //   simulation.force("charge", d3.forceManyBody()
  //     .strength(function (d) {
  //       return d.fixed ? 0 : -100;
  //     }))
  // }, 1000);

  if (forcesConfig === 0) {
    simulation
      // .force("center", d3.forceCenter(width / 2, height / 2))
      .force("centerX", d3.forceX(width / 2).strength(0.05))
      .force("centerY", d3.forceY(height / .6).strength(0.05))
      .force("level", d3.forceY().y(function (d) {
        return d.level * 200 + 50;
      }).strength(.2))
      .force("group", d3.forceManyBody()
        .strength(function (d) {
          return d.fixed ? 0 : -250;
        })
      )
  } else if (forcesConfig === 1) {
    simulation
      .force("center", d3.forceCenter(width / 2, height / 2))
      .force("charge", d3.forceManyBody()
        .strength(function (d) {
          return d.fixed ? 0 : -170;
        })
      )

    setTimeout(() => {
      simulation
        // .force("charge", null)
        .force("charge", d3.forceManyBody()
          .strength(function (d) {
            return d.fixed ? 0 : -150;
          }))
        .force("center", null)
    }, 1000);
  }

  const draw = function (graph) {
    svg.selectAll("*").remove();

    // Initialize node positions at the start of the screen
    if (forcesConfig === 0) {
      graph.nodes.forEach(function (d, i) {
        d.x = width / 2 + i * 50;
        d.y = height / 2 + i * 50;
      });
    }

    const link = svg.selectAll(".link")
      .data(graph.links)
      .enter().append("g")
      .attr("class", d => {
        if (d.disabled === true) return "link";
        return "link animated";
      });

    link.append("path")
      .style("stroke", function (d) {
        if (d.disabled === true) return "#ccc";
        return d.strength < 0 ? "red" : "green";
      })
      .style("stroke-width", function (d) {
        if (d.disabled === true) return 1;
        return Math.max(1, Math.abs(d.strength) * 3);
      });

    // First, append a rect behind the link text.
    link.append("rect")
      .style("fill", "#fff")
      .style("stroke", "none");

    // Then, append the text.
    link.append("text")
      .attr("text-anchor", "middle")
      .style("fill", function (d) {
        if (d.disabled === true) return "#ccc";
        return d.strength < 0 ? "red" : "green";
      })
      .style("font-size", "12px")
      .text(function (d) {
        return Math.round(d.strength * 100) / 100.0
      });

    const node = svg.selectAll(".node")
      .data(graph.nodes)
      .enter()
      .append("g")
      .attr("class", "node")
      .call(d3.drag()
        .on("start", function (d) {
          if (!d3.event.active) simulation.alphaTarget(0.1).restart();
          d3.select(this).classed("fixed", true);
          d.fixed = true;
          d.fx = d.x;
          d.fy = d.y;
          d3.select(this).select("circle")
            .style("stroke", "rgba(0, 0, 0, 0.7)")
            .style("stroke-width", 1);
        })
        .on("drag", function (d) {
          d.fx = Math.max(radius(d.group), Math.min(width - radius(d.group), d3.event.x));
          d.fy = Math.max(radius(d.group), Math.min(height - radius(d.group), d3.event.y));
        })
        .on("end", function (d) {
          if (!d3.event.active) simulation.alphaTarget(0);
          if (!d3.select(this).classed("fixed")) {
            d.fx = null;
            d.fy = null;
          }
        }))
      .on("click", function (d) {
        d3.select(this).classed("fixed", false);
        d.fixed = false;
        d.fx = null;
        d.fy = null;
        d3.select(this).select("circle")
          .style("stroke", null)
          .style("stroke-width", null);
      });

    node.append("circle")
      .attr("r", function (d) {
        return radius(d.group);
      })
      .style("fill", function (d) {
        return color(d.group);
      });

    node.append("text")
      .attr("dy", ".35em")
      .attr("text-anchor", "middle")
      .style("font-size", function (d) {
        return Math.max(6, Math.min(18, radius(d.group) * 3 / d.label.length)) + "px";
      })
      .text(function (d) {
        return d.label;
      });

    simulation
      .nodes(graph.nodes)
      .on("tick", tick);

    simulation.force("link")
      .links(graph.links);

    function tick() {
      link.select("path").attr("d", function (d) {
        let x1 = d.source.x,
          y1 = d.source.y,
          x2 = d.target.x,
          y2 = d.target.y,
          dx = x2 - x1,
          dy = y2 - y1,
          dr = Math.sqrt(dx * dx + dy * dy) * 1.5,

          // Defaults for normal edge.
          drx = dr,
          dry = dr,
          xRotation = 0, // degrees
          largeArc = 0, // 1 or 0
          sweep = 1; // 1 or 0

        // Self edge.
        if (x1 === x2 && y1 === y2) {
          // Fiddle with this angle to get loop oriented.
          xRotation = -45;

          // Needs to be 1.
          largeArc = 1;

          // Change sweep to change orientation of loop.
          //sweep = 0;

          // Make drx and dry different to get an ellipse
          // instead of a circle.
          drx = 20;
          dry = 15;

          // For whatever reason the arc collapses to a point if the beginning
          // and ending points of the arc are the same, so kludge it.
          x2 = x2 + 1;
          y2 = y2 + 1;
        }

        return "M" + x1 + "," + y1 + "A" + drx + "," + dry + " " + xRotation + "," + largeArc + "," + sweep + " " + x2 + "," + y2;
      });

      link.each(function (d) {
        const groupSel = d3.select(this);
        const txt = groupSel.select("text");
        const bg = groupSel.select("rect");

        // Calculate the midpoint without any offset
        const midX = (d.source.x + d.target.x) / 2;
        const midY = (d.source.y + d.target.y) / 2;

        // Position text in the middle of the link
        txt.attr("x", midX).attr("y", midY);

        // Position rect
        const bbox = txt.node().getBBox();
        bg.attr("x", bbox.x - 2)
          .attr("y", bbox.y - 2)
          .attr("width", bbox.width + 4)
          .attr("height", bbox.height + 4);

        // Bring label elements to front
        groupSel.selectAll("rect, text").raise();
      });

      node.attr("transform", function (d) {
        return "translate(" + d.x + "," + d.y + ")";
      });

      node.each(function (d) {
        if (d3.select(this).classed("fixed")) {
          d.x = d.fx;
          d.y = d.fy;
          d.vx = 0;
          d.vy = 0;
        }
        d.x = Math.max(radius(d.group), Math.min(width - radius(d.group), d.x));
        d.y = Math.max(radius(d.group), Math.min(height - radius(d.group), d.y));
      });
    }
  };

  const graph = typeof json === "string" ? JSON.parse(json) : json;
  draw(graph);
}
