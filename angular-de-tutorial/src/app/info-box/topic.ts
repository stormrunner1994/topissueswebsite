export class Topic {
    name: string;
    votes: number;
   
    constructor(name: string, votes: number = 0) {
      this.name = name;
      this.votes = 0;
    }
}
