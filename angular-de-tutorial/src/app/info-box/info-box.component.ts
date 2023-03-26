import { Component, OnInit } from '@angular/core';
import { Topic } from './topic';

@Component({
  selector: 'app-info-box',
  templateUrl: './info-box.component.html',
  styleUrls: ['./info-box.component.scss']
})
export class InfoBoxComponent implements OnInit {
someFunction() {
throw new Error('Method not implemented.');
}

  private _topiclist: Array<Topic>;

  public get topiclist(): Array<Topic> {
    return this._topiclist;
  }
  public set topiclist(value: Array<Topic>) {
    this._topiclist = value;
  }

  constructor() {}



  ngOnInit() {
    this._topiclist.push(new Topic("Climate",20));
    this._topiclist.push(new Topic("test",23));
    this._topiclist.push(new Topic("test",10));
  }
}
