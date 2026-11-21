function art(a,b,c,shape){
  const s=shape===1?`<circle cx="520" cy="190" r="90" fill="${c}"/><path d="M0 420 L180 230 L320 360 L450 270 L800 480 L0 480Z" fill="${a}" opacity=".85"/>`
  :shape===2?`<rect x="120" y="90" width="260" height="260" rx="130" fill="${c}"/><rect x="330" y="170" width="300" height="200" fill="${a}" opacity=".8"/>`
  :`<circle cx="200" cy="260" r="150" fill="${c}"/><circle cx="480" cy="220" r="110" fill="${a}" opacity=".85"/><circle cx="600" cy="360" r="70" fill="#fff" opacity=".5"/>`;
  const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="800" height="480" viewBox="0 0 800 480"><defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${b}"/><stop offset="1" stop-color="${a}"/></linearGradient></defs><rect width="800" height="480" fill="url(#g)"/>${s}</svg>`;
  return 'data:image/svg+xml;charset=utf-8,'+encodeURIComponent(svg);
}
export const fixtures = {
 users:[{id:'u-001',name:'Алиса Королёва',description:'Фотограф, цветокоррекция',storage_link:'s3://lab-bucket/alice',created_at:'2025-03-14T10:22:00Z',stat_id:'st-77'},
  {id:'u-002',name:'Максим Орлов',description:'Дизайнер интерфейсов'},{id:'u-003',name:'Нина Петрова',description:'Иллюстратор'},{id:'u-004',name:'Денис Волков',description:'Ретушёр'}],
 storages:[{id:'s-1',link:'s3://lab-bucket/alice/originals',created_at:'2025-03-14T10:25:00Z'}],
 images:[
  {id:1,storage_id:'s-1',image_link:art('#e8744f','#f6c177','#fff2c9',1),size:2840000,height:480,width:800,project_id:1},
  {id:2,storage_id:'s-1',image_link:art('#2f6f8f','#9ad0e0','#f5e6c8',2),size:1920000,height:480,width:800,project_id:2},
  {id:3,storage_id:'s-1',image_link:art('#7a4fa3','#f2a1c4','#ffe3a3',3),size:3310000,height:480,width:800,project_id:3},
  {id:4,storage_id:'s-1',image_link:art('#3f8f5a','#d4e8a0','#fff7d6',1),size:1470000,height:480,width:800,project_id:4}],
 projects:[{id:1,user_id:'u-001',source_id:1,version_id:3},{id:2,user_id:'u-001',source_id:2,version_id:5},{id:3,user_id:'u-001',source_id:3,version_id:6},{id:4,user_id:'u-001',source_id:4,version_id:7}],
 versions:[
  {id:1,operations:{},project_id:1,result_file_id:'f-101',created_at:'2025-10-02T09:10:00Z'},
  {id:2,operations:{brightness:110,contrast:105},project_id:1,result_file_id:'f-102',created_at:'2025-10-02T09:40:00Z'},
  {id:3,operations:{brightness:112,contrast:118,saturate:130,hue:-12},project_id:1,result_file_id:'f-103',created_at:'2025-10-03T18:05:00Z'},
  {id:4,operations:{},project_id:2,result_file_id:'f-201',created_at:'2025-10-10T12:00:00Z'},
  {id:5,operations:{grayscale:100,contrast:125,rotate:90},project_id:2,result_file_id:'f-202',created_at:'2025-10-11T08:30:00Z'},
  {id:6,operations:{sepia:70,blur:1.5,saturate:90},project_id:3,result_file_id:'f-301',created_at:'2025-10-20T14:15:00Z'},
  {id:7,operations:{brightness:95,saturate:150,flipX:true},project_id:4,result_file_id:'f-401',created_at:'2025-10-28T19:45:00Z'}],
 friendships:[{id:1,user_id:'u-001',friend_id:'u-002',created_at:'2025-04-01T00:00:00Z'},{id:2,user_id:'u-001',friend_id:'u-003',created_at:'2025-06-12T00:00:00Z'},{id:3,user_id:'u-001',friend_id:'u-004',created_at:'2025-09-30T00:00:00Z'}]
};
