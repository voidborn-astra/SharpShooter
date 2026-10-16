// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

namespace SharpShooter;

internal static class ChartScript
{
    internal const string Source = """
        <script>
        const colors=['#a18aff','#66f2a3','#64a7ff','#ffcb6b','#ff829e','#53d7dd'];
        const visible=series.map(()=>true), canvas=document.getElementById('chart');
        series.forEach((s,i)=>{
          const button=document.createElement('button');
          button.textContent=s.name; button.style.borderBottom='3px solid '+colors[i%colors.length];
          button.onclick=()=>{visible[i]=!visible[i];button.style.opacity=visible[i]?1:.4;draw();};
          document.getElementById('legend').append(button);
        });
        function draw(){
          const ratio=devicePixelRatio||1,w=canvas.clientWidth,h=320;
          canvas.width=w*ratio;canvas.height=h*ratio;
          const c=canvas.getContext('2d');c.scale(ratio,ratio);c.clearRect(0,0,w,h);
          let maxX=1,maxY=60,minX=Infinity;
          series.forEach((s,i)=>{if(visible[i])s.points.forEach(p=>{maxX=Math.max(maxX,p[0]);minX=Math.min(minX,p[0]);maxY=Math.max(maxY,p[1]);});});
          if(!Number.isFinite(minX))minX=0;maxX=Math.max(minX+1,maxX);
          const x=t=>55+(t-minX)/(maxX-minX)*(w-80),y=v=>25+(1-v/maxY)*255;
          c.font='12px system-ui';c.fillStyle='#c0c5cf';c.fillText('Submitted FPS · elapsed seconds · click a run to hide/show',55,15);
          for(let i=0;i<=4;i++){const v=maxY*i/4;c.strokeStyle='#343441';c.beginPath();c.moveTo(55,y(v));c.lineTo(w-25,y(v));c.stroke();c.fillText(v.toFixed(0),8,y(v)+4);}
          c.strokeStyle='#66f2a3';c.setLineDash([5,5]);c.beginPath();c.moveTo(55,y(60));c.lineTo(w-25,y(60));c.stroke();c.setLineDash([]);
          series.forEach((s,i)=>{if(!visible[i])return;c.strokeStyle=colors[i%colors.length];c.lineWidth=1.5;c.beginPath();let prior=null;
            s.points.forEach(p=>{if(prior===null||p[0]-prior>10)c.moveTo(x(p[0]),y(p[1]));else c.lineTo(x(p[0]),y(p[1]));prior=p[0];});c.stroke();});
          c.fillStyle='#c0c5cf';c.fillText(minX.toFixed(0)+'s',55,305);c.fillText(maxX.toFixed(0)+'s',w-60,305);
        }
        addEventListener('resize',draw);draw();
        </script>
        """;
}
