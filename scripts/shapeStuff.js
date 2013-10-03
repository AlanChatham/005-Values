
// Right now, mouse movement is making rotation happen instantly to the next piece
// Meanwhile, when no mouse movement, animates to the wrong place, then snaps when it stamps...

var timePerShape = 700;
var COLOR_FADE_TIME = 5000;




var shapeXMLArray = new Array();
var MAX_SHAPES = 50;

var cursorDimension = 500;

var shapeHolder = new Array();
var cursorArray = new Array();

var shapeNameArray = new Array();
shapeNameArray[0] = "objects/flower1_pink.svg";
shapeNameArray[1] = "objects/flower1_blue.svg";
shapeNameArray[2] = "objects/gem1_blue.svg";
shapeNameArray[3] = "objects/gem1_yellow.svg";
shapeNameArray[4] = "objects/gem2_green.svg";
shapeNameArray[5] = "objects/gem2_orange.svg";
shapeNameArray[6] = "objects/flower2_pink.svg";
shapeNameArray[7] = "objects/flower2_green.svg";
shapeNameArray[8] = "objects/hex_pink.svg";
shapeNameArray[9] = "objects/hex_orange.svg";
shapeNameArray[10] = "objects/hex_green.svg";
shapeNameArray[11] = "objects/hex_yellow.svg";
shapeNameArray[12] = "objects/hex_blue.svg";
shapeNameArray[13] = "objects/circle_blue.svg";
shapeNameArray[14] = "objects/circle_pink.svg";
shapeNameArray[15] = "objects/circle_yellow.svg";
shapeNameArray[16] = "objects/circle_green.svg";
shapeNameArray[17] = "objects/circle_orange.svg";
shapeNameArray[18] = "objects/gem2_blue.svg";
shapeNameArray[19] = "objects/gem2_pink.svg";
shapeNameArray[20] = "objects/gem2_yellow.svg";
shapeNameArray[21] = "objects/gem1_orange.svg";

var bgColorsArray = new Array("#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#fef3d6", "#f3deb7", "#e4df8e", "#efc2d9", "#a1b2e2"); // yellow, pink, purple


var shapeArray = new Array();

var paper;


function Stamp(idNumber){
  this.stampCanvas = document.createElement('div');
    //this.stampCanvas.position = "absolute";
  this.stampCanvas.style.width = cursorDimension;
  this.stampCanvas.style.height = cursorDimension;
  this.stampCanvas.style.position = "absolute";
  this.stampCanvas.style.top = "60px";
  this.stampCanvas.style.left = 0;
  this.stampCanvas.className = "stampCanvas";
  this.stampCanvas.id = "stampCanvas" + idNumber;

  document.body.appendChild(this.stampCanvas) ;
  this.stampPaper = new Raphael(this.stampCanvas, cursorDimension, cursorDimension); 

  // get random shape for first and next
  this.randomShapeArrayIndex = Math.floor(Math.random()*shapeXMLArray.length);
  this.randomNextShapeArrayIndex = Math.floor(Math.random()*shapeArray.length);
  // get random color to fade to
  this.randomBGColor = bgColorsArray[Math.floor(Math.random()*bgColorsArray.length)];
  // get random size
  this.randomScale = Math.random() + 1;
  // Give it an initial shape
  this.path = this.stampPaper.path(shapeArray[this.randomShapeArrayIndex]);
  
  this.path.attr({fill : '#5360ab', stroke : '#5360ab'});
  this.path.attr({"fill-opacity" : 0.4});
  this.path.attr({"stroke-opacity" : 1.0});
  this.position = { X: 0, Y: 0};
  this.randomAngle = Math.random()*360;
  // Stuff for storing and deleting shapes from the field
  this.oldShapeIndex = 0;
  this.oldShapeHolder = new Array();
  // Raphael animation holder
  // repositionAnimation handles keeping the transforming object centered
  this.repositionAnimation;
  this.rotationAnimation;
  // moveAnimation handles the larger movements
  this.moveAnimation;
  
  this.boundingBox = this.path.getBBox();
  
  this.pathOffset = { x: this.position.x - this.boundingBox.x - (this.boundingBox.width / 2),
                      y: this.position.y - this.boundingBox.y - (this.boundingBox.height / 2)};
}

Stamp.prototype.stamp = function()
{ // GET SHAPE TO STAMP
  // We're loading up a new copy of the stamp.path's shape to draw it to the canvas
	var stampedShape = paper.importSVG(shapeXMLArray[this.randomShapeArrayIndex]); 
  // GET SHAPE BOUNDING BOX
  var bBox = stampedShape.getBBox();
  // PUT UNDER CURSOR
  var offsetX = this.position.x - bBox.x - (bBox.width / 2);
  var offsetY = this.position.y - bBox.y - (bBox.height / 2);
  stampedShape.transform("t " + offsetX + ", " + offsetY + " r" + this.randomAngle + " s" + this.randomScale);  
  
  // FADE STAMPED SHAPE TO BACKGROUND COLOR
  stampedShape.animate({fill: this.randomBGColor, stroke: '#5360ab'}, COLOR_FADE_TIME, '<>');
  this.manageStampedShapes(stampedShape);
  
  // Now change the path's shape
  this.changeShape();
};

Stamp.prototype.manageStampedShapes = function(stampedShape)
{
  //And store this in a cue for eventual deletion so we can run it forever
  if (this.oldShapeHolder[this.oldShapeIndex % MAX_SHAPES] != undefined){
    this.oldShapeHolder[this.oldShapeIndex % MAX_SHAPES].remove();
  }
  this.oldShapeHolder[this.oldShapeIndex % MAX_SHAPES] = stampedShape;
  this.oldShapeIndex++;
};

Stamp.prototype.move = function(x, y, offset){
  if(typeof(offset)==='undefined') offset = 0;
  // Move the stamped shape position target
  this.position.x = x + offset;
  this.position.y = y + offset;
  // Move the layer holding our shape cursor
  this.stampCanvas.style.left = x - cursorDimension/2 + offset + "px";
  this.stampCanvas.style.top = y - cursorDimension/2 + offset + "px";
};

Stamp.prototype.changeShape = function(){
  // Bring our cursor to the front
  this.path.toFront();
  
  // And tween to the next shape
  var nextShape = this.stampPaper.path(shapeArray[this.randomNextShapeArrayIndex]);
  this.randomAngle = Math.random()*360;  
  this.randomScale = Math.random() + 1;

  nextShape.transform("r"+ this.randomAngle);
  this.boundingBox = nextShape.getBBox();
  // Don't display nextShape yet
  nextShape.remove();
  this.pathOffset.x = - this.boundingBox.x - (this.boundingBox.width / 2);
  this.pathOffset.y = - this.boundingBox.y - (this.boundingBox.height / 2);
  
  var offsetX = this.pathOffset.x + cursorDimension/2;
  var offsetY = this.pathOffset.y + cursorDimension/2;

  // Set up animations to move it and to shape tween it
  this.repositionAnimation = Raphael.animation({ transform: "t" + offsetX + ", " + offsetY + 
                                                " r" + this.randomAngle + " ," + -this.pathOffset.x + " ," + -this.pathOffset.y +
                                                " s" + this.randomScale}
                                         , timePerShape / 2, '<>');
   var tweenAnimation = Raphael.animation({ path: shapeArray[this.randomNextShapeArrayIndex] }, timePerShape - 100, '<>');
  
// Right now, we've got an issue where the animation tries to move it to the new position
  this.path.animate(this.repositionAnimation);
  this.path.animate(tweenAnimation);

  // Finally, get a new set of random shapes and directions

  this.randomShapeArrayIndex = this.randomNextShapeArrayIndex;
  this.randomNextShapeArrayIndex = Math.floor(Math.random()*shapeXMLArray.length);
  this.randomBGColor = bgColorsArray[Math.floor(Math.random()*bgColorsArray.length)];
  
};


window.onload = function() {  

  // Set up our arrays with our SVG path data
  shapeArray[0] = flower1String;
  shapeArray[1] = flower1String;
  shapeArray[2] = gem1String;
  shapeArray[3] = gem1String;
  shapeArray[4] = gem2String;
  shapeArray[5] = gem2String;
  shapeArray[6] = flower2PinkString;
  shapeArray[7] = flower2GreenString;
  shapeArray[8] = hexString;
  shapeArray[9] = hexString;
  shapeArray[10] = hexString;
  shapeArray[11] = hexString;
  shapeArray[12] = hexString;
  shapeArray[13] = circleString;
  shapeArray[14] = circleString;
  shapeArray[15] = circleString;
  shapeArray[16] = circleString;
  shapeArray[17] = circleString;
  shapeArray[18] = gem2String;
  shapeArray[19] = gem2String;
  shapeArray[20] = gem2String;
  shapeArray[21] = gem1String;
    
}    

  
jQuery(document).ready(function(){

  paper = new Raphael(document.getElementById('canvas_container'), 2760, 720); 
  
  for (var i = 0; i < shapeNameArray.length; i++){
    jQuery.ajax({
      type: "GET",
      url: shapeNameArray[i],
      dataType: "xml",
      success: function(svgXML) {
        shapeXMLArray.push(svgXML);
      }
    });
  }
  console.log('Theoretically loaded shapes');
  var stamp = new Stamp(0);
  var stamp2 = new Stamp(1);
  var stamp3 = new Stamp(2);
  var stamp4 = new Stamp(3);
  var stamp5 = new Stamp(4);
  var stamp6 = new Stamp(5);
  var stamp7 = new Stamp(6);
  var stamp8 = new Stamp(7);
	
  cursorArray.push(stamp);
  cursorArray.push(stamp2);
  cursorArray.push(stamp3);
  cursorArray.push(stamp4);
 // cursorArray.push(stamp5);
 // cursorArray.push(stamp6);
 // cursorArray.push(stamp7);
//  cursorArray.push(stamp8);
  setInterval(function(){
    for ( var i = 0; i < cursorArray.length; i++){
      cursorArray[i].stamp();
      var l = Math.random() * 2650  - cursorDimension/2;
      var t = Math.random() * 720 - cursorDimension/2;
      $("#stampCanvas" + i).animate({top: t + "px", left: l + "px"}, timePerShape / 2);
      cursorArray[i].position.x = l + cursorDimension/2;
      cursorArray[i].position.y = t + cursorDimension/2;
    } 
  } , timePerShape + i);
});

$(document).bind('mousemove', function(e){
  console.log('moved');
  // Stop all jquery animations
  $(".stampCanvas").stop();

    e = e || window.event; 
    for ( var i = 0; i < cursorArray.length; i++){
      cursorArray[i].move(e.pageX, e.pageY, i*50);
    }
});

socket = io.connect('http://127.0.0.1', { port: 8081, rememberTransport: false});
socket.on('connect', function() {
    // sends to socket.io server the host/port of oscServer
    // and oscClient
    socket.emit('config-OSC',
        {
            server: {
                port: 7115,
                host: '127.0.0.1'
            },
            client: {
                port: 3334,
                host: '127.0.0.1'
            }
        }
    );
});

socket.on('message', function(obj) { 

  // Our packets come as a #bundle, with an array as the [2] argument
  var oscMessage = obj[2];
  var x = oscMessage[1];
  var y = oscMessage[2];
  var cursorNumber = oscMessage[4];
  
  // Now adjust the values from meters to pixels
  //  4 screens of roughly 2m x 2m, for 8m x 2m
  // x ranges from .3 to 8.0
  x = -(x - .5) * 345 + 2560;
  y = -y * .9 * 360 + 460;
  
  // Stop the others from moving
  $(".stampCanvas").stop();
  
  if (cursorNumber < cursorArray.length){
    //console.log("got cursor " + cursorNumber + ", X: " + x + ", Y: " + y);
    cursorArray[cursorNumber].move(x, y);
  }
});




var flower1String = "M337.964,406.22c-0.132,0.378-0.272,0.752-0.418,1.123 c8.229,6.544,10.48,12.288,10.48,12.288s-6.165,0.929-16.036-2.997c-0.258,0.304-0.516,0.606-0.784,0.9 c5.448,8.921,5.598,15.053,5.598,15.053s-6.084-1.226-13.99-8.247c-0.011-0.05-0.024-0.104-0.035-0.153 c-0.027,0.015-0.054,0.031-0.081,0.046c0.038,0.035,0.078,0.073,0.116,0.107c2.227,10.542,0.207,16.533,0.207,16.533 s-5.343-3.259-10.391-12.655c0.021-0.146,0.036-0.302,0.055-0.449c-0.087,0.016-0.174,0.031-0.262,0.047 c0.069,0.131,0.138,0.273,0.207,0.402c-1.505,10.718-5.471,15.676-5.471,15.676s-4.037-5.067-5.507-16.004 c-0.373-0.059-0.743-0.128-1.111-0.198c-5.127,9.795-10.665,13.182-10.665,13.182s-2.019-5.991,0.209-16.533 c0.134-0.119,0.272-0.254,0.408-0.376c-0.095-0.054-0.189-0.108-0.284-0.163c-0.04,0.178-0.087,0.363-0.124,0.539 c-7.907,7.021-13.991,8.247-13.991,8.247s0.166-6.366,5.917-15.558c-0.183-0.208-0.367-0.415-0.544-0.629 c-10.193,4.184-16.595,3.23-16.595,3.23s2.369-6.01,11.065-12.736c-0.088-0.234-0.171-0.472-0.254-0.709 c-11.109,0.479-16.853-2.635-16.853-2.635s4.347-4.918,15.008-8.267c0-0.075,0.005-0.149,0.006-0.224 c-10.666-3.349-15.014-8.269-15.014-8.269s5.772-3.136,16.943-2.634c0.055-0.152,0.111-0.304,0.168-0.455 c-8.344-6.59-10.636-12.401-10.636-12.401s6.183-0.92,16.068,3.016c0.111-0.135,0.23-0.264,0.343-0.396 c-6.098-9.49-6.272-16.114-6.272-16.114s6.399,1.289,14.604,8.801c0.018,0.084,0.041,0.175,0.059,0.26 c0.044-0.026,0.09-0.051,0.134-0.077c-0.064-0.059-0.13-0.125-0.193-0.183c-2.166-10.43-0.172-16.353-0.172-16.353 s5.36,3.277,10.416,12.709c0.475-0.095,0.952-0.181,1.434-0.256c1.526-10.57,5.434-15.474,5.434-15.474s3.944,4.936,5.458,15.596 c0.018,0.003,0.035,0.006,0.052,0.009c5.037-9.34,10.352-12.584,10.352-12.584s1.976,5.876-0.146,16.224 c0.024,0.014,0.049,0.028,0.074,0.042c8.157-7.432,14.506-8.715,14.506-8.715s-0.169,6.39-5.95,15.608 c0.198,0.224,0.396,0.449,0.588,0.679c9.555-3.679,15.501-2.792,15.501-2.792s-2.183,5.542-10.058,11.939 c0.116,0.295,0.232,0.588,0.341,0.887c10.679-0.333,16.192,2.664,16.192,2.664s-4.149,4.683-14.26,8.021 c0.003,0.165,0.013,0.327,0.013,0.492c0,0.076-0.005,0.151-0.006,0.227c10.105,3.339,14.253,8.02,14.253,8.02 S348.59,406.534,337.964,406.22z";

var gem1String = "M340.658,383.622l6.059,20.57l-16.632,27.297 l-24.991,4.126l-34.218-30.847l-4.899-14.802l10.474-25.952l25.568-8.266L340.658,383.622z";

var gem2String = "M345.702,425.243l-27.463-44.892l5.707-6.851l7.42-3.751h26.994 l8.254,3.751l6.551,7.247L345.702,425.243z";

var flower2PinkString = "M398.539,388.726c-0.49-29.951-7.855-53.355-7.855-53.355s-0.164-1.064-0.573-2.455 c-0.409-1.392-1.309-1.473-1.309-1.473s-0.818-1.556-1.801-3.355c-0.981-1.801-1.555-2.946-1.555-2.946s0-1.8,0-7.938 c0-6.137-2.291-11.375-2.782-11.293s0.327,2.783,0.981,5.564c0.655,2.783,0,6.711,0,6.711s-0.981-2.782-3.273-7.119 c-2.291-4.338-6.955-8.92-7.364-8.511s3.519,5.401,5.237,9.083c1.718,3.683,1.555,6.547,1.555,6.547s-1.719-1.882-2.946-3.273 c-1.228-1.391-4.991-5.4-4.991-5.4s-0.246-3.028-1.31-5.686c-1.064-2.658-8.593-7.326-9.493-7.081 c-0.899,0.245-0.654,1.228-0.245,3.437c0.409,2.21,0.736,3.438,0.736,3.438s-5.892-1.801-7.855-1.964 c-1.965-0.163-3.438,0.9-4.747,0.9s-4.255-2.946-9.165-3.028s-9.247,3.273-10.229,3.928c-0.981,0.655-9.819,4.256-9.738,6.629 c0.082,2.373,13.257-1.063,16.776-1.882c3.519-0.818,7.119-4.01,7.119-4.01s0.409-0.327,1.801-0.327 c1.391,0,2.209,0.245,2.209,0.245s-1.882,1.063-3.601,1.31c-1.719,0.245-4.501,3.273-6.056,3.682 c-1.555,0.41-4.582,2.537-7.774,3.52c-3.191,0.982-5.073,3.109-5.073,4.992c0,1.882,5.073,0.654,8.184-0.082 c3.109-0.736,8.265-2.619,8.265-2.619s1.555-0.082,3.52-0.245c1.964-0.163,4.828-2.374,4.828-2.374s-3.355,6.793-4.91,11.212 s-1.637,8.102,1.391,9.819c3.028,1.719,7.202-5.81,7.202-5.81s0,0.491,0.572,0.818c0.573,0.327,0.9,1.228,0.9,1.228 s-0.573,0.982,0,1.555c0.572,0.572,1.555,0,1.555,0s0.818,0.736,0,1.473s-1.228,3.028-2.21,4.828 c-0.981,1.801-0.736,4.502-3.354,6.793c-2.619,2.291-5.564,3.109-5.074,3.355c0.491,0.245,2.946,0.163,4.992-0.573 c2.046-0.737,3.846-2.455,3.846-2.455s-0.163,1.555-0.49,2.864c-0.328,1.309,0.081,3.682,1.391,3.682s0.9-3.436,0.9-3.436 s0.573,0,1.31-0.164s1.18-0.573,1.18-0.573s1.766,3.437,2.912,3.765c1.146,0.327,2.291-0.9,2.291-1.392s-1.063,0.082-1.964-0.081 c-0.9-0.164-1.392-1.801-1.392-1.801s1.228,0.082,2.373-0.654c0.871-0.561,1.064-3.191,0.573-3.52 c-0.491-0.327-1.882,2.046-2.291,1.965c-0.409-0.082,0-2.947,0.327-5.156s2.455-5.482,2.455-5.482s0.572,0.818,0.572,1.555 s1.964,3.355,2.619,3.273c0.654-0.082,0.654-1.555,0.491-3.438c-0.164-1.882-1.31-4.256-1.31-4.256s0.818-3.027,1.228-3.928 s3.765-2.373,6.465-3.928s2.291-2.291,2.291-2.291s0.327-0.409,3.191,3.027c2.864,3.438,4.747,9.084,4.747,9.084l-1.146,0.49 c0,0,0,0-1.146,0.41c-1.146,0.408-1.555,0.9-1.555,0.9s-1.146-0.164-3.765,0.163c-2.618,0.327-5.728,6.547-6.874,10.229 c-1.146,3.682-2.618,8.51-3.354,9.002c-0.737,0.49-1.801-0.328-2.21-0.818c-0.409-0.492,1.31-2.292,0.164-2.783 c-1.146-0.49-1.064,1.719-2.455,1.719c-1.392,0-1.637,0.982-1.883,2.946c-0.245,1.964,1.719,2.536,1.719,2.536 s-0.572,0.655-1.555,1.555c-0.982,0.9-3.601,4.01-3.601,4.01l-0.409-0.981c0,0-2.537,0.082-3.683,0.818 c-1.146,0.737-1.391,2.374-1.391,2.374s-0.328,0.49-1.31,0.408c-0.982-0.082-0.818-2.373-0.818-2.373s0.818-0.409,1.31-1.146 c0.49-0.736,0.409-2.128-0.328-2.128c-0.736,0-0.49,1.064-0.899,1.392s-0.655,0.409-1.556,0.491 c-0.899,0.082-0.736,1.309-0.736,1.309s-1.473,1.965-1.555,3.928c-0.082,1.965,1.964,4.256,4.337,4.828 c2.373,0.574,4.747-0.163,4.747-0.163s-0.082,1.964-0.409,3.273c-0.328,1.31,0.572,4.173,1.882,4.991s3.519,0.573,3.519,0.573 s0.246,0.737,1.064,0.737s1.637-2.047,1.063-2.701s-1.146,0.246-1.146,0.246s-0.082-0.982-0.327-1.474 c-0.246-0.491-1.275,0.573-1.275,0.573s-0.198-0.982,0-2.537s0.866-2.209,0.866-2.209s2.127,0.899,4.255-0.082 c2.128-0.982,0.982-3.438,0.982-3.438s1.473-1.146,3.109-2.127c1.637-0.982,3.355-3.355,3.355-3.355s0.572,0.49,0.981,0.982 c0.409,0.49,2.701,1.145,4.174,1.227s2.291-1.309,2.046-1.964s-1.063-0.736-1.063-0.736s-1.146,0.491-1.474-0.491 s0.082-2.209,0.082-2.209s0.654,1.882,3.683,1.719c3.027-0.164,2.209-4.584,1.31-4.584c-0.9,0-0.246,2.455-1.228,2.783 c-0.982,0.327-1.064-0.9-1.064-0.9s0.655-1.309,1.228-2.291c0.573-0.982,0.736-1.31,0.736-1.31s2.984,0.298,5.975,2.292 c4.173,2.781,5.728,5.973,5.728,5.973s0.246-0.981,1.146,20.786c0.899,21.768-1.065,32.484-1.965,55.315 c-0.9,22.832,2.292,30.119,2.292,30.119l2.864,3.273c0,0-1.801-13.666-1.883-22.586C395.267,443.718,399.03,418.677,398.539,388.726 z M366.297,317.939c-0.736,0.245,0.082-0.9-0.981-0.982c-1.064-0.081-3.683,3.273-3.683,3.765c0,0.49-0.736,0.982-1.473,0.736 c-0.737-0.246-4.992,4.828-4.992,4.828s2.373-5.237,4.582-7.201c2.21-1.964,3.602-3.928,3.602-3.928l3.846,2.291 C367.197,317.448,367.033,317.694,366.297,317.939z M370.962,314.339c-1.064,0.245-2.537,2.045-2.537,2.045 s-0.736-0.654-1.228-1.227c-0.491-0.573-3.273-0.573-3.273-0.573s0.409-0.899,0.9-1.882s1.31-4.501,1.31-4.501 s1.146,0.491,2.782,2.21c1.637,1.718,2.7,3.354,2.7,3.354S372.025,314.093,370.962,314.339z M387.901,358.446 c-2.292-1.637-5.542-2.902-5.811-2.945s-0.409,0.245-1.31-0.573c-0.899-0.817-2.291,0.164-2.291,0.164s1.064-2.128,1.555-3.683 c0.491-1.555,4.01-8.348,4.174-10.803s-1.801-3.272-1.801-3.272s0.491-1.063,1.392-1.392c0.9-0.327,3.027,1.555,3.027,1.555 s2.374,5.074,3.847,12.685s1.801,11.456,1.801,11.456S390.192,360.083,387.901,358.446z";
var flower2GreenString = flower2PinkString;
var hexString = "M368.465,377.905l-8.786,4.923l-8.655-5.147l0.129-10.069 l8.786-4.923l8.655,5.147L368.465,377.905z";
var circleString = "M368.123,371.248c0,4.854-3.937,8.791-8.791,8.791 s-8.791-3.937-8.791-8.791c0-4.855,3.937-8.791,8.791-8.791S368.123,366.393,368.123,371.248z";
