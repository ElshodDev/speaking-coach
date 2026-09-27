import type { SpeakingTopic, WritingTopic } from './types';

// Speaking va Writing mashqi uchun tayyor mavzular (A2–C1).
// Kategoriyalar: everyday, IELTS Part 1/2/3, CEFR Multilevel (O'zbekiston).
// Har bir `text` ≤ 300 belgi (server cheklovi).

export const SPEAKING_TOPICS_BANK: SpeakingTopic[] = [
  // ───────────── A2 ─────────────
  { id: 'sp-a2-01', level: 'A2', category: 'everyday', text: 'Describe your favourite place to relax at the weekend. Where is it, and what do you do there?' },
  { id: 'sp-a2-02', level: 'A2', category: 'everyday', text: 'Tell me about your daily routine. What time do you get up, and what do you usually do in the morning and in the evening?' },
  { id: 'sp-a2-03', level: 'A2', category: 'everyday', text: 'Talk about your favourite food. What is it, how is it made, and when do you usually eat it?' },
  { id: 'sp-a2-04', level: 'A2', category: 'everyday', text: 'Describe your house or flat. How many rooms are there, and which room do you like best? Why?' },
  { id: 'sp-a2-05', level: 'A2', category: 'everyday', text: 'What did you do last weekend? Tell me about the people you saw and the places you went to.' },
  { id: 'sp-a2-06', level: 'A2', category: 'everyday', text: 'Talk about your best friend. How did you meet, what does he or she look like, and what do you do together?' },
  { id: 'sp-a2-07', level: 'A2', category: 'ielts-part1', text: 'Do you like cooking? What do you usually cook? Who taught you to cook?' },
  { id: 'sp-a2-08', level: 'A2', category: 'ielts-part1', text: 'Where do you live? Do you like your neighbourhood? What can people do there in their free time?' },
  { id: 'sp-a2-09', level: 'A2', category: 'ielts-part1', text: 'Do you like the weather in your city? What is your favourite season, and why?' },
  { id: 'sp-a2-10', level: 'A2', category: 'ielts-part1', text: 'How do you usually travel to school or work? How long does it take? Do you enjoy the journey?' },
  { id: 'sp-a2-11', level: 'A2', category: 'cefr', text: 'What do you like doing in your free time? How often do you do it, and who do you do it with?' },
  { id: 'sp-a2-12', level: 'A2', category: 'cefr', text: 'Tell me about your family. How many people are there, and who do you spend the most time with?' },
  { id: 'sp-a2-13', level: 'A2', category: 'cefr', text: 'Which do you prefer: spending a holiday in the mountains or in a big city? Give two reasons for your choice.' },
  { id: 'sp-a2-14', level: 'A2', category: 'cefr', text: 'How do people in your family celebrate Navruz? What do you eat, and what do you do on that day?' },
  {
    id: 'sp-a2-15',
    level: 'A2',
    category: 'ielts-part2',
    text: 'Describe a shop you like to visit.',
    points: ['where it is', 'what it sells', 'how often you go there', 'and explain why you like it'],
  },

  // ───────────── B1 ─────────────
  { id: 'sp-b1-01', level: 'B1', category: 'everyday', text: 'Tell me about a skill you would like to learn in the future. Why is it interesting to you, and how would you learn it?' },
  { id: 'sp-b1-02', level: 'B1', category: 'everyday', text: 'Describe a typical day at a market or bazaar in your town. What can you buy there, and what is the atmosphere like?' },
  { id: 'sp-b1-03', level: 'B1', category: 'everyday', text: 'Talk about a time you helped a neighbour or someone in your mahalla. What happened, and how did you feel afterwards?' },
  { id: 'sp-b1-04', level: 'B1', category: 'everyday', text: 'How has your town changed in the last few years? Talk about new buildings, roads, shops or parks, and say whether the changes are good.' },
  { id: 'sp-b1-05', level: 'B1', category: 'ielts-part1', text: 'Do you use your phone a lot? What apps do you use most often? Have you ever tried to spend a day without your phone?' },
  { id: 'sp-b1-06', level: 'B1', category: 'ielts-part1', text: 'Do you enjoy reading? What kind of things do you read? Did you read more when you were a child?' },
  { id: 'sp-b1-07', level: 'B1', category: 'ielts-part1', text: 'Do you like doing sport? Which sports are popular in your country? Would you like to try a new sport?' },
  { id: 'sp-b1-08', level: 'B1', category: 'ielts-part1', text: 'Do you prefer to wake up early or late? Has your sleeping routine changed since you were younger? Why?' },
  {
    id: 'sp-b1-09',
    level: 'B1',
    category: 'ielts-part2',
    text: 'Describe a memorable meal you had with other people.',
    points: ['where and when you had it', 'who you were with', 'what you ate', 'and explain why this meal was memorable'],
  },
  {
    id: 'sp-b1-10',
    level: 'B1',
    category: 'ielts-part2',
    text: 'Describe a trip you took that you really enjoyed.',
    points: ['where you went', 'how you travelled', 'what you did there', 'and explain why you enjoyed it so much'],
  },
  {
    id: 'sp-b1-11',
    level: 'B1',
    category: 'ielts-part2',
    text: 'Describe a teacher who helped you a lot.',
    points: ['who this teacher was', 'what subject he or she taught', 'how he or she helped you', 'and explain why you still remember this teacher'],
  },
  { id: 'sp-b1-12', level: 'B1', category: 'cefr', text: 'Do you think it is better to study alone or in a group? Give reasons and examples from your own experience.' },
  { id: 'sp-b1-13', level: 'B1', category: 'cefr', text: 'Some people prefer to live in a village, while others prefer a big city like Tashkent. Which would you choose, and why?' },
  { id: 'sp-b1-14', level: 'B1', category: 'cefr', text: 'Some people think that children should have a mobile phone from the age of eight. Discuss the advantages and disadvantages.' },
  { id: 'sp-b1-15', level: 'B1', category: 'cefr', text: 'What job would you like to have in ten years? What do you need to do now to get this job?' },

  // ───────────── B2 ─────────────
  { id: 'sp-b2-01', level: 'B2', category: 'ielts-part1', text: 'Do you often take photos? What do you usually photograph? Do you ever print your photos or keep them only on your phone?' },
  { id: 'sp-b2-02', level: 'B2', category: 'ielts-part1', text: 'How do you usually spend your money? Do you prefer to save or to spend? Did your parents teach you how to manage money?' },
  {
    id: 'sp-b2-03',
    level: 'B2',
    category: 'ielts-part2',
    text: 'Describe a time when you had to make a difficult decision.',
    points: ['what the decision was', 'when you made it', 'what options you had', 'and explain how you feel about the decision now'],
  },
  {
    id: 'sp-b2-04',
    level: 'B2',
    category: 'ielts-part2',
    text: 'Describe a website or app that you find very useful.',
    points: ['what it is', 'how you found out about it', 'what you use it for', 'and explain why it is so useful to you'],
  },
  {
    id: 'sp-b2-05',
    level: 'B2',
    category: 'ielts-part2',
    text: 'Describe a historical place in your country that you have visited.',
    points: ['where it is', 'when you went there', 'what you saw and learnt', 'and explain what impression it made on you'],
  },
  {
    id: 'sp-b2-06',
    level: 'B2',
    category: 'ielts-part2',
    text: 'Describe a goal you achieved that you are proud of.',
    points: ['what the goal was', 'how long it took you', 'what difficulties you faced', 'and explain why you are proud of achieving it'],
  },
  { id: 'sp-b2-07', level: 'B2', category: 'ielts-part3', text: 'Why do some young people prefer to work for large companies, while others want to start their own business? Which is the better choice today?' },
  { id: 'sp-b2-08', level: 'B2', category: 'ielts-part3', text: 'How has the way people shop changed in recent years? Do you think traditional markets will disappear in the future?' },
  { id: 'sp-b2-09', level: 'B2', category: 'ielts-part3', text: 'What can ordinary people do to reduce air pollution in cities? Is it more the responsibility of individuals or of large organisations?' },
  { id: 'sp-b2-10', level: 'B2', category: 'ielts-part3', text: 'Why do you think team sports are so popular? What can children learn from playing in a team?' },
  { id: 'sp-b2-11', level: 'B2', category: 'ielts-part3', text: 'How important is it for tourists to learn about local customs before visiting a country? What problems can happen if they do not?' },
  { id: 'sp-b2-12', level: 'B2', category: 'cefr', text: 'Some people believe that homework should be banned in primary schools. Discuss the arguments for and against this idea and give your opinion.' },
  { id: 'sp-b2-13', level: 'B2', category: 'cefr', text: 'Online courses are becoming more popular than traditional classes. Compare the two and say which is more effective for learning a language.' },
  { id: 'sp-b2-14', level: 'B2', category: 'cefr', text: 'Some people think that fast food should be more expensive to protect public health. Discuss the advantages and disadvantages of this idea.' },
  { id: 'sp-b2-15', level: 'B2', category: 'cefr', text: 'Many families in Uzbekistan hold very large weddings. Some say this is an important tradition; others think it is a waste of money. Discuss both sides.' },

  // ───────────── C1 ─────────────
  {
    id: 'sp-c1-01',
    level: 'C1',
    category: 'ielts-part2',
    text: 'Describe a piece of advice that changed the way you think.',
    points: ['who gave you the advice', 'what the advice was', 'in what situation you received it', 'and explain how it changed your way of thinking'],
  },
  {
    id: 'sp-c1-02',
    level: 'C1',
    category: 'ielts-part2',
    text: 'Describe a scientific discovery or invention that you think has had a big impact on society.',
    points: ['what it is', 'when and how it appeared', 'how people use it', 'and explain why you think its impact has been so significant'],
  },
  {
    id: 'sp-c1-03',
    level: 'C1',
    category: 'ielts-part2',
    text: 'Describe a situation in which you disagreed with someone but later changed your mind.',
    points: ['who the person was', 'what you disagreed about', 'what made you change your mind', 'and explain what you learnt from the experience'],
  },
  {
    id: 'sp-c1-04',
    level: 'C1',
    category: 'ielts-part2',
    text: 'Describe a tradition in your community that you would like future generations to keep.',
    points: ['what the tradition is', 'when and how it is practised', 'who takes part in it', 'and explain why you think it should be preserved'],
  },
  { id: 'sp-c1-05', level: 'C1', category: 'ielts-part3', text: 'To what extent should governments invest in space exploration when there are still serious problems to solve on Earth?' },
  { id: 'sp-c1-06', level: 'C1', category: 'ielts-part3', text: 'Artificial intelligence can now write texts and create images. How might this change the kinds of jobs people do over the next twenty years?' },
  { id: 'sp-c1-07', level: 'C1', category: 'ielts-part3', text: 'Is it possible for a country to develop its economy quickly without harming the environment? What compromises might be necessary?' },
  { id: 'sp-c1-08', level: 'C1', category: 'ielts-part3', text: 'Some argue that social media has made people more informed; others say it has made them more easily misled. How would you evaluate these views?' },
  { id: 'sp-c1-09', level: 'C1', category: 'ielts-part3', text: 'Why do some cultural traditions survive for centuries while others disappear? What role does globalisation play in this process?' },
  { id: 'sp-c1-10', level: 'C1', category: 'ielts-part3', text: 'Should university education focus mainly on preparing students for employment, or on developing their ability to think critically? Why?' },
  { id: 'sp-c1-11', level: 'C1', category: 'ielts-part3', text: 'As people live longer, how should societies adapt their healthcare, housing and pension systems to support an ageing population?' },
  { id: 'sp-c1-12', level: 'C1', category: 'cefr', text: 'Some people believe that famous athletes are paid far too much compared with doctors and teachers. Discuss the arguments for and against and give your own view.' },
  { id: 'sp-c1-13', level: 'C1', category: 'cefr', text: 'Working from home should become the norm for office jobs. Discuss the advantages and disadvantages for employees, companies and cities.' },
  { id: 'sp-c1-14', level: 'C1', category: 'cefr', text: 'Some say that tourism helps preserve historic cities such as Samarkand and Bukhara, while others argue it damages their character. Evaluate both views.' },
  { id: 'sp-c1-15', level: 'C1', category: 'cefr', text: 'Compare learning from books with learning from real-life experience. Which do you think shapes a person more, and in what circumstances?' },
];

export const WRITING_TOPICS_BANK: WritingTopic[] = [
  // ───────────── A2 ─────────────
  { id: 'wr-a2-01', level: 'A2', category: 'paragraph', text: 'Write about a person in your family you admire. Say who they are, what they are like, and why you admire them.', words: [60, 100] },
  { id: 'wr-a2-02', level: 'A2', category: 'paragraph', text: 'Write about your favourite day of the week. What do you usually do on that day, and why do you like it?', words: [60, 100] },
  { id: 'wr-a2-03', level: 'A2', category: 'paragraph', text: 'Describe your town or village. Where is it, what can you see there, and what do you like about it?', words: [60, 100] },
  { id: 'wr-a2-04', level: 'A2', category: 'paragraph', text: 'Write about a holiday or festival you enjoy, for example Navruz. What do people eat and do, and how do you feel on that day?', words: [60, 100] },
  { id: 'wr-a2-05', level: 'A2', category: 'letter-informal', text: 'Your friend is coming to visit you next weekend. Write an email to your friend. Say what you will do together, what food you will eat, and what to bring.', words: [50, 70] },
  { id: 'wr-a2-06', level: 'A2', category: 'letter-informal', text: 'You have started a new English course. Write a message to a friend. Tell them about your teacher, your classmates and what you like about the course.', words: [50, 70] },
  { id: 'wr-a2-07', level: 'A2', category: 'letter-informal', text: 'Your friend wants to start doing sport but does not know which one. Write an email to your friend and suggest a sport. Explain why it is a good choice.', words: [50, 70] },
  { id: 'wr-a2-08', level: 'A2', category: 'letter-formal', text: 'You want to join a summer sports camp. Write a short email to the camp manager. Introduce yourself, say which sport you are interested in, and ask about the price and dates.', words: [70, 100] },

  // ───────────── B1 ─────────────
  { id: 'wr-b1-01', level: 'B1', category: 'paragraph', text: 'Write about a book, film or series that you recently enjoyed. Describe what it is about and explain why you would recommend it.', words: [100, 150] },
  { id: 'wr-b1-02', level: 'B1', category: 'paragraph', text: 'Describe an experience that taught you an important lesson. What happened, and what did you learn from it?', words: [100, 150] },
  { id: 'wr-b1-03', level: 'B1', category: 'paragraph', text: 'Some people say that plov is more than just food in Uzbekistan. Write a paragraph explaining when and why people cook plov and what it means to them.', words: [100, 150] },
  { id: 'wr-b1-04', level: 'B1', category: 'letter-informal', text: 'Your friend is worried because they have an important exam next month. Write a letter to your friend. Give advice on how to prepare and how to stay calm.', words: [50, 70] },
  { id: 'wr-b1-05', level: 'B1', category: 'letter-informal', text: 'You recently moved to a new flat. Write a letter to a friend. Describe the flat and the neighbourhood, say what you like and dislike, and invite your friend to visit.', words: [120, 150] },
  { id: 'wr-b1-06', level: 'B1', category: 'letter-formal', text: 'You bought headphones online, but they stopped working after a week. Write a letter to the shop. Explain the problem, describe what you have tried, and say what you want them to do.', words: [120, 150] },
  { id: 'wr-b1-07', level: 'B1', category: 'letter-formal', text: 'The park near your home has no lights, and it is dangerous in the evening. Write a letter to the local administration. Describe the problem and suggest a solution.', words: [120, 150] },
  { id: 'wr-b1-08', level: 'B1', category: 'essay-opinion', text: 'Some people think that students should wear a school uniform. Do you agree or disagree? Give reasons and examples.', words: [150, 200] },

  // ───────────── B2 ─────────────
  { id: 'wr-b2-01', level: 'B2', category: 'essay-opinion', text: 'Some people believe that every student should learn to cook at school. To what extent do you agree or disagree?', words: [250, 300] },
  { id: 'wr-b2-02', level: 'B2', category: 'essay-opinion', text: 'Nowadays many people spend several hours a day on their phones. Some say this is harmful to their relationships with family and friends. Do you agree or disagree?', words: [250, 300] },
  { id: 'wr-b2-03', level: 'B2', category: 'essay-opinion', text: 'Public transport in cities should be completely free. To what extent do you agree or disagree with this statement?', words: [250, 300] },
  { id: 'wr-b2-04', level: 'B2', category: 'essay-discussion', text: 'Some people prefer to travel with an organised tour group, while others like to plan their own trip. Discuss both views and give your own opinion.', words: [250, 300] },
  { id: 'wr-b2-05', level: 'B2', category: 'essay-discussion', text: 'More and more young people move from villages to big cities to find work. What problems does this cause, and what can be done to solve them?', words: [250, 300] },
  { id: 'wr-b2-06', level: 'B2', category: 'essay-discussion', text: 'Many teenagers have part-time jobs while they are still at school. Discuss the advantages and disadvantages of this.', words: [250, 300] },
  { id: 'wr-b2-07', level: 'B2', category: 'letter-formal', text: 'You recently stayed at a hotel and were unhappy with the service. Write a letter to the hotel manager. Describe what went wrong, explain how it affected your stay, and say what you expect the hotel to do.', words: [150, 200] },
  { id: 'wr-b2-08', level: 'B2', category: 'letter-informal', text: 'A foreign friend plans to visit Uzbekistan for a week and has asked for your advice. Write a letter recommending places to visit, food to try and things to be careful about.', words: [150, 200] },

  // ───────────── C1 ─────────────
  { id: 'wr-c1-01', level: 'C1', category: 'essay-opinion', text: 'Some argue that the rapid development of artificial intelligence will do more harm than good to the job market. To what extent do you agree or disagree?', words: [250, 320] },
  { id: 'wr-c1-02', level: 'C1', category: 'essay-opinion', text: 'Governments should spend more money on preventing illness, for example by promoting healthy lifestyles, than on treating people who are already sick. To what extent do you agree?', words: [250, 320] },
  { id: 'wr-c1-03', level: 'C1', category: 'essay-opinion', text: 'Museums and historical sites should be free for everyone, even if this means that the government has to pay for them. Do you agree or disagree?', words: [250, 320] },
  { id: 'wr-c1-04', level: 'C1', category: 'essay-discussion', text: 'Some people believe that scientific research should be funded mainly by governments, while others think private companies should pay for it. Discuss both views and give your own opinion.', words: [250, 320] },
  { id: 'wr-c1-05', level: 'C1', category: 'essay-discussion', text: 'Consumers today throw away huge amounts of clothing and electronics. What are the causes of this, and what measures could be taken to reduce the waste?', words: [250, 320] },
  { id: 'wr-c1-06', level: 'C1', category: 'essay-discussion', text: 'In many countries, people are choosing to have fewer children or to start a family later in life. Discuss the advantages and disadvantages of this trend for society.', words: [250, 320] },
  { id: 'wr-c1-07', level: 'C1', category: 'essay-discussion', text: 'Some believe that news media should simply report facts, while others think journalists should also explain and interpret events. Discuss both views and give your opinion.', words: [250, 320] },
  { id: 'wr-c1-08', level: 'C1', category: 'letter-formal', text: 'Your city plans to replace a popular green area with a car park. Write a letter to the city council. Explain why the area is valuable, outline the likely consequences of the plan, and propose an alternative.', words: [180, 220] },
];
